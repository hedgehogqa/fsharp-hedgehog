namespace Hedgehog.Stateful

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful

/// Abstract base class for sequential state machine tests.
/// The SUT (System Under Test) is created externally and passed to Check/ToProperty methods.
/// Setup and cleanup are modeled as command lists that execute before/after test actions.
[<AbstractClass>]
type SequentialSpecification<'TSystem, 'TState>() =

    /// The initial model state
    abstract member InitialState : 'TState

    /// Range of action sequence lengths to generate
    abstract member Range : Range<int>

    /// Commands that operate on the SUT during the test sequence.
    /// The SUT is passed as a typed parameter to Execute methods.
    abstract member Commands : ICommand<'TSystem, 'TState> array

    /// Setup commands that execute before the test sequence.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Setup commands cannot be shrunk away from the action sequence.
    /// Default is an empty list (no setup).
    abstract member SetupCommands : ICommand<'TSystem, 'TState> array
    default _.SetupCommands = [||]

    /// Cleanup commands that execute after the test sequence.
    /// These commands are generated (parameters can shrink) but always execute in the order specified.
    /// Cleanup commands cannot be shrunk away and are guaranteed to run even if tests fail.
    /// Cleanup is generated using the final state after setup and test actions complete.
    /// Default is an empty list (no cleanup).
    abstract member CleanupCommands : ICommand<'TSystem, 'TState> array
    default _.CleanupCommands = [||]

    /// Convert this specification to a property that can be checked.
    /// The SUT must be created externally and passed as a parameter.
    /// This generates test sequences with setup/cleanup and executes them against the SUT.
    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq

        let gen = Sequential.genActions this.Range setupActions testActions cleanupActions this.InitialState

        property {
            let! actions = gen
            do! Sequential.executeWithSUT sut actions
        }

    /// Convert this specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    member this.ToPropertyWith(createSut: unit -> 'TSystem) : Property<unit> =
        let setupActions = this.SetupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let testActions = this.Commands |> Seq.map _.ToActionGen() |> List.ofSeq
        let cleanupActions = this.CleanupCommands |> Seq.map _.ToActionGen() |> List.ofSeq
        let gen = Sequential.genActions this.Range setupActions testActions cleanupActions this.InitialState

        property {
            let! actions = gen
            let sut = createSut()  // Create fresh SUT for this test run
            do! Sequential.executeWithSUT sut actions
        }
