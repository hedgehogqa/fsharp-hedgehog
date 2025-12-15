namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful.FSharp

[<RequireQualifiedAccess>]
module Sequential =

    let internal genActions
        (range: Range<int>)
        (setupActions: ActionGen<'TSystem, 'TState> list)
        (testActions: ActionGen<'TSystem, 'TState> list)
        (cleanupActions: ActionGen<'TSystem, 'TState> list)
        (initial: 'TState)
        : Gen<Actions<'TSystem, 'TState>> =

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
                            failwithf $"Required setup/cleanup command cannot generate in the current state (Category: %A{category})"
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
                    // Select one of the available action generators
                    let selectedGenTree = Gen.toRandom (Gen.item available)
                    let selectedGen = Random.run seed size selectedGenTree

                    // Generate the action tree from the selected generator
                    let seed1, seed2 = Seed.split seed
                    let actionAndEnvTree = Gen.toRandom (Tree.outcome selectedGen)
                    let actionAndEnvTreeResult = Random.run seed1 size actionAndEnvTree

                    // Extract outcome for state evolution
                    let action, env' = Tree.outcome actionAndEnvTreeResult
                    let name = fst (Env.freshName env)
                    let outputVar = Var.bound name
                    let state' = action.Update state outputVar

                    // Recurse with split seed
                    let restTrees, envFinal =
                        Random.run seed2 size (collectTrees (n - 1) state' env')

                    // Map the tree to just extract the action (discard env for tree structure)
                    let actionTree = Tree.map fst actionAndEnvTreeResult
                    (actionTree :: restTrees), envFinal
        )

        // Validate a sequence of actions
        let rec validateSequence env state counter = function
            | [] -> true
            | action :: rest ->
                if action.Require env state then
                    let state' = action.Update state (Var.bound (Name counter))
                    validateSequence env state' (counter + 1) rest
                else false

        // Project final state by applying Updates from action lists
        let projectFinalState (setupActions: Action<'TSystem, 'TState> list) (testActions: Action<'TSystem, 'TState> list) (initial: 'TState) : 'TState =
            let mutable state = initial
            let mutable counter = 0
            for action in setupActions do
                state <- action.Update state (Var.bound (Name counter))
                counter <- counter + 1
            for action in testActions do
                state <- action.Update state (Var.bound (Name counter))
                counter <- counter + 1
            state

        // Main generator
        gen {
            let! count = Gen.int32 range

            return! Gen.ofRandom (Random (fun seed size ->
                // Split seed for setup, test, and cleanup
                let seed1, seed2 = Seed.split seed
                let seed2a, seed2b = Seed.split seed2

                // Generate setup actions
                let setupTree, envAfterSetup = Random.run seed1 size (genFixedActionsRandom ActionCategory.Setup
                                                                          setupActions initial Env.empty)

                // Compute state after setup
                let setupActions = Tree.outcome setupTree
                let stateAfterSetup = projectFinalState setupActions [] initial

                // Generate test actions
                let actionTrees, _ = Random.run seed2a size (collectTrees count stateAfterSetup envAfterSetup)

                let testActionsTree =
                    if List.isEmpty actionTrees then
                        Tree.singleton []
                    else
                        actionTrees
                        |> Shrink.sequenceList
                        |> Tree.filter (validateSequence envAfterSetup stateAfterSetup 0)

                // Compute final state using outcomes
                let testActionsOutcome = Tree.outcome testActionsTree
                let finalStateOutcome = projectFinalState setupActions testActionsOutcome initial

                // Generate cleanup actions based on final state
                let cleanupTree, _ = Random.run seed2b size (genFixedActionsRandom ActionCategory.Cleanup cleanupActions
                                                                 finalStateOutcome envAfterSetup)

                // Combine all three trees
                let combinedTree =
                    setupTree |> Tree.bind (fun setupShrunk ->
                        testActionsTree |> Tree.bind (fun testShrunk ->
                            cleanupTree |> Tree.map (fun cleanupShrunk ->
                                { Initial = initial; Steps = setupShrunk @ testShrunk @ cleanupShrunk }
                            )
                        )
                    )

                combinedTree
            ))
        }

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
                            do! Property.exn ex

                        | ActionResult.Success output ->
                            let name, env' = Env.freshName env
                            let outputVar = Var.bound name
                            let env'' = Env.add outputVar output env'
                            let state0 = state
                            let state1 = action.Update state outputVar

                            do! Property.counterexample (fun () -> formatActionName action)
                            // Ensure returns bool - wrap it in a property that handles exceptions
                            let ensureProperty =
                                try
                                    action.Ensure env'' state0 state1 output |> Property.ofBool
                                with ex ->
                                    Property.exn ex
                            do! ensureProperty
                            do! loop state1 env'' rest
                    }

        property {
            do! Property.counterexample (fun () -> $"Final state: %A{actions.Initial}")
            do! loop actions.Initial Env.empty actions.Steps
        }
