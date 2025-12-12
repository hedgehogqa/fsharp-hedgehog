module Hedgehog.Stateful.Tests.CounterFixtureClean

open Hedgehog.FSharp
open Hedgehog.Linq
open Hedgehog.Stateful
open Xunit
type Counter() =
    let mutable value = 0
    member _.Increment() = value <- value + 1
    member _.Decrement() = value <- value - 1
    member _.AddRandom() =
        let rnd = System.Random().Next(100)
        value <- value + rnd
        value
    member _.Set(n: int) =
        value <- n
    member _.Get() = value
    member _.Reset() = value <- 0

type CounterState = {
    CurrentCount: Var<int>  // Symbolic reference to current count
}

/// Increment command - SUT is a typed parameter!
type IncrementCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Increment"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) =
        counter.Increment()
        let result = counter.Get()  // Return new value
        System.Threading.Tasks.Task.FromResult(result)
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount + 1

type AddRandomCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "AddRandom"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = System.Threading.Tasks.Task.FromResult(counter.AddRandom())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// Decrement command
type DecrementCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Decrement"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) =
        counter.Decrement()
        let result = counter.Get()  // Return new value
        System.Threading.Tasks.Task.FromResult(result)

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount - 1

/// Reset command
type ResetCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Reset"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) =
        counter.Reset()
        let result = counter.Get()  // Return new value (should be 0)
        System.Threading.Tasks.Task.FromResult(result)

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(_, _, _, _, result) =
        // Reset always sets to 0
        result = 0

/// Set command - demonstrates the bug detection
type SetCommand() =
    inherit Command<Counter, CounterState, int, int>()

    override _.Name = "Set"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, value) =
        counter.Set(value)
        let result = counter.Get()
        System.Threading.Tasks.Task.FromResult(result)
    override _.Generate _ = Gen.int32 (Range.linearFrom 0 -10 100)
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(_, _, _, input, result) =
        Assert.Equal(result, input)
        result = input

/// Get command - returns a value
type GetCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Get"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = System.Threading.Tasks.Task.FromResult(counter.Get())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        result = oldState.CurrentCount.Resolve(env)

/// SequentialSpecification that manages Counter lifecycle
type CounterSpec() =
    inherit SequentialSpecification<Counter, CounterState>()

    override _.SetupCommands = [|
        ResetCommand()
    |]

    // Initial model state - unbound var with default value 0
    override _.InitialState = { CurrentCount = Var.symbolic 0 }

    // Generate sequences of 1-50 actions
    override _.Range = Range.linear 1 50

    // Commands - no SUT needed in constructors!
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
        ResetCommand()
        GetCommand()
        SetCommand()
        AddRandomCommand()
    |]


[<Fact>]
let ``Counter test with clean SUT parameter API``() =
    let sut = Counter()
    // Use CheckWith to create a fresh Counter for each property test run
    CounterSpec().ToProperty(sut).Check()
