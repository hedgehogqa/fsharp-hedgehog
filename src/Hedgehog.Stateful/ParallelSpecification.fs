namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp

/// Abstract base class for parallel state machine tests.
/// Tests concurrent execution by running two branches in parallel after a sequential prefix.
/// The SUT (System Under Test) is created externally and passed to Check/ToProperty methods.
[<AbstractClass>]
type ParallelSpecification<'TSystem, 'TState>() =

    /// The initial model state
    abstract member InitialState : 'TState

    /// Range of prefix sequence lengths to generate (sequential actions before parallel execution)
    abstract member PrefixRange : Range<int>

    /// Range of branch sequence lengths to generate (parallel actions in each branch)
    abstract member BranchRange : Range<int>

    /// Commands that operate on the SUT during the test sequence.
    /// The SUT is passed as a typed parameter to Execute methods.
    abstract member Commands : ICommand<'TSystem, 'TState> array

    /// Setup commands that execute before the prefix and parallel branches.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Setup commands cannot be shrunk away from the action sequence.
    /// Default is an empty list (no setup).
    abstract member SetupCommands : ICommand<'TSystem, 'TState> array
    default _.SetupCommands = [||]

    /// Cleanup commands that execute after the parallel branches complete.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Cleanup commands cannot be shrunk away and are guaranteed to run even if tests fail.
    /// Cleanup is generated using the state after prefix (before parallel execution).
    /// Default is an empty list (no cleanup).
    abstract member CleanupCommands : ICommand<'TSystem, 'TState> array
    default _.CleanupCommands = [||]

    /// Convert this specification to a property that can be checked.
    /// The SUT must be created externally and passed as a parameter.
    /// This generates parallel test sequences and executes them against the SUT.
    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let gen = Parallel.genActions this.PrefixRange this.BranchRange setupActions testActions cleanupActions this.InitialState

        property {
            let! actions = gen
            do! Parallel.executeWithSUT sut actions
        }

    /// Convert this specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    member this.ToPropertyWith(createSut: unit -> 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let gen = Parallel.genActions this.PrefixRange this.BranchRange setupActions testActions cleanupActions this.InitialState

        property {
            let! actions = gen
            let sut = createSut()  // Create fresh SUT for this test run
            do! Parallel.executeWithSUT sut actions
        }
