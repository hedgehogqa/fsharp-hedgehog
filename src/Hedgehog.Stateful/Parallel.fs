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
                    let name, _ = Env.freshName Env.empty
                    a.Update s (Var.bound name)
                ) initial

            // Generate prefix using state after setup
            let! prefix = Sequential.genActions prefixRange [] testActions [] stateAfterSetup

            // Calculate state after prefix
            let stateAfterPrefix =
                prefix.Steps
                |> List.fold (fun s a ->
                    let name, _ = Env.freshName Env.empty
                    a.Update s (Var.bound name)
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

    let interleavings (xs: 'a list) (ys: 'a list) : 'a list seq =
        let rec loop xs ys =
            seq {
                match xs, ys with
                | [], [] -> yield []
                | x :: xs', [] -> yield! loop xs' [] |> Seq.map (fun rest -> x :: rest)
                | [], y :: ys' -> yield! loop [] ys' |> Seq.map (fun rest -> y :: rest)
                | x :: xs', y :: ys' ->
                    yield! loop xs' ys |> Seq.map (fun rest -> x :: rest)
                    yield! loop xs ys' |> Seq.map (fun rest -> y :: rest)
            }
        loop xs ys

    let internal checkLinearization
        (initial: 'TState)
        (prefix: Action<'TSystem, 'TState> list)
        (interleaving: Action<'TSystem, 'TState> list)
        (results: Map<string, obj>)
        : bool =
        let rec runActions state env actions =
            match actions with
            | [] -> Some (state, env)
            | a :: rest ->
                if not (a.Precondition state && a.Require env state) then None
                else
                    match Map.tryFind a.Name results with
                    | None -> None
                    | Some output ->
                        let name, env' = Env.freshName env
                        let outputVar = Var.bound name
                        let env'' = Env.add outputVar output env'
                        let state' = a.Update state outputVar
                        runActions state' env'' rest

        match runActions initial Env.empty prefix with
        | None -> false
        | Some (stateAfterPrefix, envAfterPrefix) ->
            runActions stateAfterPrefix envAfterPrefix interleaving
            |> Option.isSome

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
                    return! Property.exn ex
                | ActionResult.Success output ->
                    let name, env' = Env.freshName env
                    let outputVar = Var.bound name
                    env <- Env.add outputVar output env'
                    state <- action.Update state outputVar

            // Run prefix sequentially
            let prefixResults = ResizeArray<string * obj>()

            for action in actions.Prefix do
                let! result = Property.ofTask (action.Execute sut env state)

                match result with
                | ActionResult.Failure ex ->
                    do! Property.counterexample (fun () -> formatActionName action)
                    return! Property.exn ex
                | ActionResult.Success output ->
                    prefixResults.Add(action.Name, output)
                    let name, env' = Env.freshName env
                    let outputVar = Var.bound name
                    env <- Env.add outputVar output env'
                    state <- action.Update state outputVar

            // Run branches in parallel
            let runBranch (branch: Action<'TSystem, 'TState> list) : Async<Result<(string * obj) list, exn>> =
                async {
                    let results = ResizeArray<string * obj>()
                    let mutable branchEnv = env
                    let mutable branchState = state
                    let mutable error = None

                    for action in branch do
                        match error with
                        | Some _ -> ()
                        | None ->
                            let! result = action.Execute sut branchEnv branchState |> Async.AwaitTask

                            match result with
                            | ActionResult.Failure ex -> error <- Some ex
                            | ActionResult.Success output ->
                                results.Add(action.Name, output)
                                let name, env' = Env.freshName branchEnv
                                let outputVar = Var.bound name
                                branchEnv <- Env.add outputVar output env'
                                branchState <- action.Update branchState outputVar

                    return
                        match error with
                        | Some ex -> Error ex
                        | None -> Ok (results |> Seq.toList)
                }

            let! branchResults =
                async {
                    return! Async.Parallel [runBranch actions.Branch1; runBranch actions.Branch2]
                }

            let results = branchResults : Result<(string * obj) list, exn> array

            // Check linearizability regardless of branch success/failure
            // Then run cleanup actions
            let linearizabilityCheck =
                match results[0], results[1] with
                | Error ex, _ | _, Error ex ->
                    // Still need to run cleanup even on failure
                    Property.exn ex
                | Ok results1, Ok results2 ->
                    let allResults =
                        [ yield! prefixResults; yield! results1; yield! results2 ]
                        |> Map.ofList

                    let isLinearizable =
                        interleavings actions.Branch1 actions.Branch2
                        |> Seq.exists (fun interleaving ->
                            checkLinearization actions.Initial actions.Prefix interleaving allResults
                        )

                    if not isLinearizable then
                        property {
                            do! Property.counterexample (fun () -> "No valid interleaving found")
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
                            let name, env' = Env.freshName env
                            let outputVar = Var.bound name
                            env <- Env.add outputVar output env'

            // Check linearizability first
            do! linearizabilityCheck

            // Then report any cleanup errors
            match cleanupError with
            | Some (actionName, ex) ->
                do! Property.counterexample (fun () -> actionName)
                do! Property.exn ex
            | None -> ()
        }
