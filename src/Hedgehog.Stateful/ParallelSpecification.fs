namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp


/// <summary>
/// Abstract base class for parallel state machine tests.
/// Tests concurrent execution by running two branches in parallel after a sequential prefix.
/// The SUT (System Under Test) is created externally and passed to Check/ToProperty methods.
/// </summary>
[<AbstractClass>]
type ParallelSpecification<'TSystem, 'TState>() =


    /// <summary>
    /// The initial model state.
    /// </summary>
    abstract member InitialState : 'TState


    /// <summary>
    /// Range of prefix sequence lengths to generate (sequential actions before parallel execution).
    /// </summary>
    abstract member PrefixRange : Range<int>


    /// <summary>
    /// Range of branch sequence lengths to generate (parallel actions in each branch).
    /// </summary>
    abstract member BranchRange : Range<int>


    /// <summary>
    /// Commands that operate on the SUT during the test sequence.
    /// The SUT is passed as a typed parameter to Execute methods.
    /// </summary>
    abstract member Commands : ICommand<'TSystem, 'TState> array


    /// <summary>
    /// Setup commands that execute before the prefix and parallel branches.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Setup commands cannot be shrunk away from the action sequence.
    /// Default is an empty list (no setup).
    /// </summary>
    abstract member SetupCommands : ICommand<'TSystem, 'TState> array
    default _.SetupCommands = [||]


    /// <summary>
    /// Cleanup commands that execute after the parallel branches complete.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Cleanup commands cannot be shrunk away and are guaranteed to run even if tests fail.
    /// Cleanup is generated using the state after prefix (before parallel execution).
    /// Default is an empty list (no cleanup).
    /// </summary>
    abstract member CleanupCommands : ICommand<'TSystem, 'TState> array
    default _.CleanupCommands = [||]

    /// <summary>
    /// Convert this specification to a property that can be checked.
    /// </summary>
    /// <param name="sut">The system under test (SUT) instance to use for the test run.</param>
    /// <returns>A property representing the parallel specification test.</returns>
    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let gen = Parallel.genActions this.PrefixRange this.BranchRange setupActions testActions cleanupActions this.InitialState

        property {
            let! actions = gen
            do! Parallel.executeWithSUT sut actions
        }

    /// <summary>
    /// Convert this specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    /// </summary>
    /// <param name="createSut">A function that creates a new SUT instance for each test run.</param>
    /// <returns>A property representing the parallel specification test.</returns>
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
