namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful


/// <summary>
/// Abstract base class for sequential state machine tests.
/// The SUT (System Under Test) is created externally and passed to Check/ToProperty methods.
/// Setup and cleanup are modeled as command lists that execute before/after test actions.
/// </summary>
[<AbstractClass>]
type SequentialSpecification<'TSystem, 'TState>() =


    /// <summary>
    /// The initial model state.
    /// </summary>
    abstract member InitialState : 'TState


    /// <summary>
    /// Range of action sequence lengths to generate.
    /// </summary>
    abstract member Range : Range<int>


    /// <summary>
    /// Commands that operate on the SUT during the test sequence.
    /// The SUT is passed as a typed parameter to Execute methods.
    /// </summary>
    abstract member Commands : ICommand<'TSystem, 'TState> array


    /// <summary>
    /// Setup commands that execute before the test sequence.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Setup commands cannot be shrunk away from the action sequence.
    /// Default is an empty list (no setup).
    /// </summary>
    abstract member SetupCommands : ICommand<'TSystem, 'TState> array
    default _.SetupCommands = [||]


    /// <summary>
    /// Cleanup commands that execute after the test sequence.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Cleanup commands cannot be shrunk away and are guaranteed to run even if tests fail.
    /// Cleanup is generated using the final state after setup and test actions complete.
    /// Default is an empty list (no cleanup).
    /// </summary>
    abstract member CleanupCommands : ICommand<'TSystem, 'TState> array
    default _.CleanupCommands = [||]

    /// <summary>
    /// Convert this specification to a property that can be checked.
    /// </summary>
    /// <param name="sut">The system under test (SUT) instance to use for the test run.</param>
    /// <returns>A property representing the sequential specification test.</returns>
    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq

        let gen = Sequential.genActions this.Range setupActions testActions cleanupActions this.InitialState Env.empty

        property {
            let! actions = gen
            do! Sequential.executeWithSUT sut actions
        }

    /// <summary>
    /// Convert this specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    /// </summary>
    /// <param name="createSut">A function that creates a new SUT instance for each test run.</param>
    /// <returns>A property representing the sequential specification test.</returns>
    member this.ToPropertyWith(createSut: unit -> 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let gen = Sequential.genActions this.Range setupActions testActions cleanupActions this.InitialState Env.empty

        gen |> Property.forAll (fun actions ->
            let sut = createSut()
            Sequential.executeWithSUT sut actions
        )
