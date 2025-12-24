namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful.FSharp

type internal ParallelActions<'TSystem, 'TState> = {
    Initial: 'TState
    Setup: Action<'TSystem, 'TState> list
    Prefix: Action<'TSystem, 'TState> list
    Branch1: Action<'TSystem, 'TState> list
    Branch2: Action<'TSystem, 'TState> list
    Cleanup: Action<'TSystem, 'TState> list
}

[<RequireQualifiedAccess>]
module Parallel =

    let internal genActions
        (prefixRange: Range<int>)
        (branchRange: Range<int>)
        (setupActions: ActionGen<'TSystem, 'TState> list)
        (testActions: ActionGen<'TSystem, 'TState> list)
        (cleanupActions: ActionGen<'TSystem, 'TState> list)
        (initial: 'TState)
        : Gen<ParallelActions<'TSystem, 'TState>> =
        gen {
            // Generate setup actions first
            let! setup = Sequential.genActions (Range.singleton 0) setupActions [] [] initial

            // Calculate state after setup
            let stateAfterSetup =
                setup.Steps
                |> List.fold (fun s a ->
                    a.Update s (Var.symbolicEmpty())
                ) initial

            // Generate prefix using state after setup
            let! prefix = Sequential.genActions prefixRange [] testActions [] stateAfterSetup

            // Calculate state after prefix
            let stateAfterPrefix =
                prefix.Steps
                |> List.fold (fun s a ->
                    a.Update s (Var.symbolicEmpty())
                ) stateAfterSetup

            // Generate branches from state after prefix
            let! branch1 = Sequential.genActions branchRange [] testActions [] stateAfterPrefix
            let! branch2 = Sequential.genActions branchRange [] testActions [] stateAfterPrefix

            // Generate cleanup based on state after prefix (not after branches, since branches run in parallel)
            let! cleanup = Sequential.genActions (Range.singleton 0) cleanupActions [] [] stateAfterPrefix

            return {
                Initial = initial
                Setup = setup.Steps
                Prefix = prefix.Steps
                Branch1 = branch1.Steps
                Branch2 = branch2.Steps
                Cleanup = cleanup.Steps
            }
        }

    /// Check if there exists a valid interleaving of two action branches.
    /// This function integrates interleaving generation with validation, allowing early
    /// termination and pruning of invalid branches for better performance.
    /// Instead of generating all O(2^(n+m)) interleavings, it searches depth-first
    /// and stops as soon as a valid interleaving is found.
    let internal isLinearizable
        (initial: 'TState)
        (prefix: Action<'TSystem, 'TState> list)
        (branch1: Action<'TSystem, 'TState> list)
        (branch2: Action<'TSystem, 'TState> list)
        (results: Map<Name, obj>)
        : bool =

        /// Try to execute a single action and return the new state/env if successful
        let tryExecuteAction state env (action: Action<'TSystem, 'TState>) =
            if not (action.Precondition state && action.Require env state) then
                None
            else
                match Map.tryFind action.Id results with
                | None -> None
                | Some output ->
                    let _, env' = Env.freshName env
                    let outputVar = Concrete output
                    let state' = action.Update state outputVar
                    Some (state', env')

        /// Recursively search for a valid interleaving of the two branches.
        /// Returns true as soon as a valid interleaving is found (early termination).
        /// Prunes branches that fail preconditions or action execution.
        let rec searchInterleavings state env xs ys =
            match xs, ys with
            | [], [] ->
                // Successfully reached the end - found a valid interleaving
                true
            | x :: xs', [] ->
                // Only branch1 actions remaining
                match tryExecuteAction state env x with
                | Some (state', env') -> searchInterleavings state' env' xs' []
                | None -> false
            | [], y :: ys' ->
                // Only branch2 actions remaining
                match tryExecuteAction state env y with
                | Some (state', env') -> searchInterleavings state' env' [] ys'
                | None -> false
            | x :: xs', y :: ys' ->
                // Both branches have actions - try both orderings
                // First try executing action from branch1
                match tryExecuteAction state env x with
                | Some (state', env') when searchInterleavings state' env' xs' ys ->
                    true  // Found valid path through branch1 first
                | _ ->
                    // Branch1 first either failed or led to invalid path, try branch2 first
                    match tryExecuteAction state env y with
                    | Some (state', env') -> searchInterleavings state' env' xs ys'
                    | None -> false

        /// First execute the prefix sequentially
        let rec runPrefix state env = function
            | [] -> Some (state, env)
            | action :: rest ->
                match tryExecuteAction state env action with
                | Some (state', env') -> runPrefix state' env' rest
                | None -> None

        // Execute prefix, then search for valid interleaving of branches
        match runPrefix initial Env.empty prefix with
        | None -> false
        | Some (state, env) -> searchInterleavings state env branch1 branch2

    let internal executeWithSUT (sut: 'TSystem) (actions: ParallelActions<'TSystem, 'TState>) : Property<unit> =
        let formatActionName (action: Action<'TSystem, 'TState>) : string =
            match action.Category with
            | ActionCategory.Setup -> $"+ %s{action.Name}"
            | ActionCategory.Test -> action.Name
            | ActionCategory.Cleanup -> $"- %s{action.Name}"

        property {
            // Run setup actions sequentially first
            let mutable state = actions.Initial
            let mutable env = Env.empty

            for action in actions.Setup do
                let! result = Property.ofTask (action.Execute sut env state)

                match result with
                | ActionResult.Failure ex ->
                    do! Property.counterexample (fun () -> formatActionName action)
                    do! Property.counterexample (fun () -> $"Final state: %A{state}")
                    return! Property.exn ex
                | ActionResult.Success output ->
                    let _, env' = Env.freshName env
                    let outputVar = Concrete output
                    env <- env'
                    state <- action.Update state outputVar

            // Run prefix sequentially
            let prefixResults = ResizeArray<Name * obj>()

            for action in actions.Prefix do
                let! result = Property.ofTask (action.Execute sut env state)

                match result with
                | ActionResult.Failure ex ->
                    do! Property.counterexample (fun () -> formatActionName action)
                    do! Property.counterexample (fun () -> $"Final state: %A{state}")
                    return! Property.exn ex
                | ActionResult.Success output ->
                    prefixResults.Add(action.Id, output)
                    let _, env' = Env.freshName env
                    let outputVar = Concrete output
                    env <- env'
                    state <- action.Update state outputVar

            // Save state and env before parallel branches (which is also before cleanup)
            let stateBeforeBranches = state
            let envBeforeBranches = env

            // Run branches in parallel
            let runBranch (branch: Action<'TSystem, 'TState> list) : Async<Result<(Name * obj) list, exn>> =
                let rec loop results branchEnv branchState = function
                    | [] -> async { return Ok (List.rev results) }
                    | action :: rest ->
                        async {
                            let! result = action.Execute sut branchEnv branchState |> Async.AwaitTask
                            match result with
                            | ActionResult.Failure ex ->
                                return Error ex
                            | ActionResult.Success output ->
                                let _, env' = Env.freshName branchEnv
                                let outputVar = Concrete output
                                let newState = action.Update branchState outputVar
                                return! loop ((action.Id, output) :: results) env' newState rest
                        }
                loop [] env state branch

            let! branchResults =
                async {
                    return! Async.Parallel [runBranch actions.Branch1; runBranch actions.Branch2]
                }

            let results = branchResults : Result<(Name * obj) list, exn> array

            // Check linearizability regardless of branch success/failure
            // Then run cleanup actions
            let linearizabilityCheck =
                match results[0], results[1] with
                | Error ex, _ | _, Error ex ->
                    // Branch failed - report state before branches
                    property {
                        do! Property.counterexample (fun () -> $"Final state: %A{stateBeforeBranches}")
                        return! Property.exn ex
                    }
                | Ok results1, Ok results2 ->
                    let allResults =
                        [ yield! prefixResults; yield! results1; yield! results2 ]
                        |> Map.ofList

                    let linearizable =
                        isLinearizable actions.Initial actions.Prefix actions.Branch1 actions.Branch2 allResults

                    if not linearizable then
                        property {
                            do! Property.counterexample (fun () -> "No valid interleaving found")
                            do! Property.counterexample (fun () -> $"Final state: %A{stateBeforeBranches}")
                            return! Property.failure
                        }
                    else
                        Property.ofBool true

            // Run cleanup actions - these always run even if tests failed
            // We catch any cleanup failures and report them after linearizability check
            let mutable cleanupError = None

            for action in actions.Cleanup do
                match cleanupError with
                | Some _ -> ()  // Skip remaining cleanup if one fails
                | None ->
                    if action.Precondition state && action.Require env state then
                        let! result = Property.ofTask (action.Execute sut env state)
                        match result with
                        | ActionResult.Failure ex ->
                            cleanupError <- Some (formatActionName action, ex)
                        | ActionResult.Success output ->
                            let _, env' = Env.freshName env
                            let outputVar = Concrete output
                            env <- env'
                            state <- action.Update state outputVar

            // Check linearizability first
            do! linearizabilityCheck

            // Then report any cleanup errors
            match cleanupError with
            | Some (actionName, ex) ->
                do! Property.counterexample (fun () -> actionName)
                do! Property.exn ex
            | None -> ()
        }
