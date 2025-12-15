module Hedgehog.Stateful.Tests.AsyncCounterSpec

open Hedgehog.FSharp
open Hedgehog.Linq
open Hedgehog.Stateful
open Xunit
open System.Threading.Tasks

/// Async Counter - all operations return Tasks
type AsyncCounter() =
    let mutable value = 0

    member _.IncrementAsync() = task {
        do! Task.Delay(1) // Simulate async work
        value <- value + 1
    }

    member _.DecrementAsync() = task {
        do! Task.Delay(1)
        value <- value - 1
    }

    member _.AddRandomAsync() = task {
        do! Task.Delay(1)
        let rnd = System.Random().Next(100)
        value <- value + rnd
        return value
    }

    member _.SetAsync(n: int) = task {
        do! Task.Delay(1)
        value <- n
    }

    member _.GetAsync() = task {
        do! Task.Delay(1)
        return value
    }

    member _.ResetAsync() = task {
        do! Task.Delay(1)
        value <- 0
    }

type AsyncCounterState = {
    CurrentCount: Var<int>
}

/// Async Increment command
type AsyncIncrementCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, unit, int>()

    override _.Name = "IncrementAsync"

    override _.Precondition _ = true

    override _.Execute(counter, _, _, _) = task {
        do! counter.IncrementAsync()
        return! counter.GetAsync()
    }

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount + 1

/// Async Decrement command
type AsyncDecrementCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, unit, int>()

    override _.Name = "DecrementAsync"

    override _.Precondition _ = true

    override _.Execute(counter, _, _, _) = task {
        do! counter.DecrementAsync()
        return! counter.GetAsync()
    }

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount - 1

/// Async Reset command
type AsyncResetCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, unit, int>()

    override _.Name = "ResetAsync"

    override _.Execute(counter, _, _, _) = task {
        do! counter.ResetAsync()
        return! counter.GetAsync()
    }

    override _.Precondition _ = true

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(_, _, _, _, result) =
        result = 0

/// Async Set command
type AsyncSetCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, int, int>()

    override _.Name = "SetAsync"

    override _.Precondition _ = true

    override _.Execute(counter, _, _, value) = task {
        do! counter.SetAsync(value)
        return! counter.GetAsync()
    }

    override _.Generate _ = Gen.int32 (Range.linearFrom 0 -10 100)
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(_, _, _, input, result) =
        result = input

/// Async Get command
type AsyncGetCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, unit, int>()

    override _.Name = "GetAsync"

    override _.Precondition _ = true

    override _.Execute(counter, _, _, _) = counter.GetAsync()

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(env, oldState, _, _, result) =
        result = oldState.CurrentCount.Resolve(env)

/// Async AddRandom command
type AsyncAddRandomCommand() =
    inherit Command<AsyncCounter, AsyncCounterState, unit, int>()

    override _.Name = "AddRandomAsync"

    override _.Precondition _ = true

    override _.Execute(counter, _, _, _) = counter.AddRandomAsync()

    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// Async Counter Specification
type AsyncCounterSpec() =
    inherit SequentialSpecification<AsyncCounter, AsyncCounterState>()

    override _.SetupCommands = [|
        AsyncResetCommand()
    |]

    override _.InitialState = { CurrentCount = Var.symbolic 0 }

    override _.Range = Range.linear 1 50

    override _.Commands = [|
        AsyncIncrementCommand()
        AsyncDecrementCommand()
        AsyncResetCommand()
        AsyncGetCommand()
        AsyncSetCommand()
        AsyncAddRandomCommand()
    |]


[<Fact>]
let ``AsyncCounter test with async operations``() =
    let sut = AsyncCounter()
    AsyncCounterSpec().ToProperty(sut).Check()
