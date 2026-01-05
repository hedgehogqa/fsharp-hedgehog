namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful.FSharp

[<RequireQualifiedAccess>]
module Sequential =

    let internal genActionsWithState
        (range: Range<int>)
        (setupActions: ActionGen<'TSystem, 'TState> list)
        (testActions: ActionGen<'TSystem, 'TState> list)
        (cleanupActions: ActionGen<'TSystem, 'TState> list)
        (initialState: 'TState)
        (initialEnv: Env)
        : Gen<Actions<'TSystem, 'TState> * 'TState * Env> =

        // Generate a fixed list of actions from command specs (for setup/cleanup)
        // The list structure is fixed, but individual action parameters can shrink
        // Returns a Random function that produces a Tree of action lists
        let rec genFixedActionsRandom
            (category: ActionCategory)
            (actions: ActionGen<'TSystem, 'TState> list)
            (state: 'TState)
            (env: Env)
            : Random<Tree<Action<'TSystem, 'TState> list> * Env> =

            Random (fun seed size ->
                // Helper to thread state through action generation
                // Returns list of (action tree, updated env) pairs and final env
                let rec generateWithStateThreading seeds currentState currentEnv = function
                    | [] -> [], currentEnv
                    | spec :: rest ->
                        match spec.TryGen category currentState currentEnv with
                        | None ->
                            failwithf $"Required %A{category} command failed to generate in current state. Command: %s{spec.GetType().Name}"
                        | Some actionGen ->
                            let seed1, seed2 = Seed.split seeds

                            // Generate action tree with environment
                            let actionAndEnvTree = Random.run seed1 size (Gen.toRandom actionGen)

                            // Use outcome to compute next state for subsequent actions
                            let outcomeAction, outcomeEnv = Tree.outcome actionAndEnvTree
                            let name, nextEnv = Env.freshName outcomeEnv
                            let outputVar = Var.bound name
                            let nextState = outcomeAction.Update currentState outputVar

                            // Recursively generate rest with updated state
                            let restTrees, finalEnv = generateWithStateThreading seed2 nextState nextEnv rest

                            // Extract just action tree (discard env from tree structure)
                            let actionTree = Tree.map fst actionAndEnvTree

                            (actionTree :: restTrees), finalEnv

                // Generate all action trees with proper state threading
                let actionTrees, finalEnv = generateWithStateThreading seed state env actions

                // Combine trees into a single tree of action lists
                // This preserves shrinking: each action can shrink independently,
                // and the tree structure ensures we test all valid combinations
                let rec combineTrees = function
                    | [] -> Tree.singleton []
                    | tree :: rest ->
                        tree |> Tree.bind (fun action ->
                            combineTrees rest |> Tree.map (fun actions ->
                                action :: actions
                            )
                        )

                combineTrees actionTrees, finalEnv
            )


        // Helper: Collect action trees with state threading (for main test commands)
        let rec collectTrees n state env = Random (fun seed size ->
            if n <= 0 then
                [], env
            else
                let available = testActions |> List.choose (fun cmd -> cmd.TryGen ActionCategory.Test state env)

                if List.isEmpty available then
                    [], env
                else
                    let seedSelect, seedRest = Seed.split seed
                    // Select one of the available action generators
                    let selectedGenTree = Gen.toRandom (Gen.item available)
                    let selectedGen = Random.run seedSelect size selectedGenTree

                    // Extract the chosen Gen (discards command selection shrinks, keeps parameter shrinks)
                    let chosenGen = Tree.outcome selectedGen

                    // Run the chosen Gen to get Tree<Action * Env> with parameter shrinking preserved
                    let seedAction, seedRecurse = Seed.split seedRest
                    let actionAndEnvTree = chosenGen |> Gen.toRandom |> Random.run seedAction size

                    // Extract outcome for state evolution
                    let action, env' = Tree.outcome actionAndEnvTree
                    let name = fst (Env.freshName env)
                    let outputVar = Var.bound name
                    let state' = action.Update state outputVar

                    // Recurse to generate remaining actions
                    let restTrees, envFinal =
                        Random.run seedRecurse size (collectTrees (n - 1) state' env')

                    // Extract just the action from the tree (discard env), preserving shrinks
                    let actionTree = Tree.map fst actionAndEnvTree
                    (actionTree :: restTrees), envFinal
        )

        // Validate a sequence of actions during shrinking and return the final state if valid.
        // Returns Some(finalState) if the sequence is valid, None if any action fails Precondition.
        // Only checks Precondition (not Require) because:
        // - During shrinking, we're still in generation phase - no execution has happened yet
        // - Require is only checked during execution when we have real values
        let rec validateSequence state = function
            | [] -> Some state
            | action :: rest ->
                if action.Precondition state then
                    let state' = action.Update state (Var.bound action.Id)
                    validateSequence state' rest
                else None

        // Filter a tree while computing and returning the projected state for valid outcomes.
        // Similar to Tree.filter but returns (filteredTree, projectedState) tuple.
        // This avoids re-projecting the state after filtering.
        let rec filterTreeWithState (initialState: 'TState) (validate: 'TState -> Action<'TSystem, 'TState> list -> 'TState option) (tree: Tree<Action<'TSystem, 'TState> list>) : Tree<Action<'TSystem, 'TState> list> * 'TState =
            let (Node (actions, shrinks)) = tree

            // Validate and project the root outcome
            match validate initialState actions with
            | None ->
                // Root outcome must always be valid (already validated during generation)
                failwith "Root outcome validation failed - this should never happen"
            | Some finalState ->
                // Recursively filter shrinks, keeping only those that are valid
                let rec filterShrinks shrinkSeq =
                    shrinkSeq
                    |> Seq.choose (fun shrinkTree ->
                        let (Node (shrunkActions, _)) = shrinkTree
                        match validate initialState shrunkActions with
                        | Some _ ->
                            let filteredShrink, _ = filterTreeWithState initialState validate shrinkTree
                            Some filteredShrink
                        | None -> None
                    )

                let filteredShrinks = filterShrinks shrinks
                (Node (actions, filteredShrinks), finalState)

        // Project the model state forward by applying Updates from action lists.
        // Used only for setup actions to compute stateAfterSetup.
        let projectState (actions: Action<'TSystem, 'TState> list) (initialState: 'TState) : 'TState =
            actions
            |> List.fold (fun state action ->
                if action.Precondition state then
                    action.Update state (Var.bound action.Id)
                else
                    state  // Skip actions that don't satisfy precondition
            ) initialState

        // Main generator
        gen {
            let! count = Gen.int32 range

            return! Gen.ofRandom (Random (fun seed size ->
                // Split seed for setup, test, and cleanup
                let seed1, seed2 = Seed.split seed
                let seed2a, seed2b = Seed.split seed2

                // Generate setup actions
                let setupTree, envAfterSetup = Random.run seed1 size (genFixedActionsRandom ActionCategory.Setup
                                                                          setupActions initialState initialEnv)

                // Project state forward through setup actions to know what state we'll be in
                // after setup, so we can generate valid test actions for that state
                let setupActionsOutcome = Tree.outcome setupTree
                let stateAfterSetup = projectState setupActionsOutcome initialState

                // Generate test actions based on the projected state after setup
                let actionTrees, envAfterTest = Random.run seed2a size (collectTrees count stateAfterSetup envAfterSetup)

                let testActionsTree, stateAfterTest =
                    if List.isEmpty actionTrees then
                        Tree.singleton [], stateAfterSetup
                    else
                        actionTrees
                        |> Shrink.sequenceList
                        |> filterTreeWithState stateAfterSetup validateSequence

                // Generate cleanup actions based on the projected final state
                let cleanupTree, envAfterCleanup = Random.run seed2b size (genFixedActionsRandom ActionCategory.Cleanup cleanupActions
                                                                 stateAfterTest envAfterTest)

                // Combine all three trees
                let combinedTree =
                    setupTree |> Tree.bind (fun setupShrunk ->
                        testActionsTree |> Tree.bind (fun testShrunk ->
                            cleanupTree |> Tree.map (fun cleanupShrunk ->
                                { Initial = initialState; Steps = setupShrunk @ testShrunk @ cleanupShrunk }
                            )
                        )
                    )

                // Return the combined tree, the final projected state, and the final environment
                combinedTree |> Tree.map (fun actions -> actions, stateAfterTest, envAfterCleanup)
            ))
        }

    // Wrapper that only returns Actions (for backward compatibility)
    let internal genActions
        (range: Range<int>)
        (setupActions: ActionGen<'TSystem, 'TState> list)
        (testActions: ActionGen<'TSystem, 'TState> list)
        (cleanupActions: ActionGen<'TSystem, 'TState> list)
        (initialState: 'TState)
        (initialEnv: Env)
        : Gen<Actions<'TSystem, 'TState>> =
        genActionsWithState range setupActions testActions cleanupActions initialState initialEnv
        |> Gen.map (fun (actions, _, _) -> actions)

    /// Execute actions with a SUT instance.
    /// The SUT is passed as a typed parameter to each command's Execute and Ensure methods.
    let internal executeWithSUT (sut: 'TSystem) (actions: Actions<'TSystem, 'TState>) : Property<unit> =
        let formatActionName (action: Action<'TSystem, 'TState>) : string =
            match action.Category with
            | ActionCategory.Setup -> $"+ %s{action.Name}"
            | ActionCategory.Test -> action.Name
            | ActionCategory.Cleanup -> $"- %s{action.Name}"

        let rec loop state env steps : Property<unit> =
            match steps with
            | [] -> Property.ofBool true
            | action :: rest ->
                if not (action.Precondition state && action.Require env state) then
                    // Skip this action and continue with the rest
                    loop state env rest
                else
                    property {
                        // Execute always returns Task<ExecutionResult<obj>>
                        let! result = Property.ofTask (action.Execute sut env state)

                        match result with
                        | ActionResult.Failure ex ->
                            // Add counterexample for the failing action before propagating the exception
                            do! Property.counterexample (fun () -> formatActionName action)
                            if action.Category <> ActionCategory.Cleanup then
                                do! Property.counterexample (fun () -> $"Failed at state: %A{state}")
                            do! Property.exn ex

                        | ActionResult.Success output ->
                            let outputVar = Var.bound action.Id
                            let env' = Env.add outputVar output env
                            let state0 = state
                            let state1 = action.Update state outputVar

                            do! Property.counterexample (fun () -> formatActionName action)
                            // Ensure returns bool - wrap it in a property that handles exceptions
                            // If ensure fails, add final state before propagating
                            do!
                                try
                                    action.Ensure env' state0 state1 output |> Property.ofBool
                                with ex ->
                                    if action.Category <> ActionCategory.Cleanup then
                                        property {
                                            do! Property.counterexample (fun () -> $"Failed at state: %A{state1}")
                                            do! Property.exn ex
                                        }
                                    else
                                        Property.exn ex
                            do! loop state1 env' rest
                    }

        property {
            do! Property.counterexample (fun () -> $"Initial state: %A{actions.Initial}")
            do! loop actions.Initial Env.empty actions.Steps
        }
