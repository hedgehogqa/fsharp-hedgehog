module Hedgehog.Stateful.Tests.ParallelCounterWithSetupCleanup

open System.Threading.Tasks
open Hedgehog.FSharp
open Hedgehog.Linq
open Hedgehog.Stateful
open Xunit

/// Thread-safe counter for parallel testing with setup/cleanup
type ThreadSafeCounterWithTracking() =
    let mutable value = 0
    let mutable setupCalled = false
    let mutable cleanupCalled = false

    member _.Increment() =
        System.Threading.Interlocked.Increment(&value)

    member _.Decrement() =
        System.Threading.Interlocked.Decrement(&value)

    member _.Get() =
        System.Threading.Interlocked.CompareExchange(&value, 0, 0)

    member _.Setup(initialValue: int) =
        setupCalled <- true
        System.Threading.Interlocked.Exchange(&value, initialValue)

    member _.Cleanup() =
        cleanupCalled <- true
        System.Threading.Interlocked.Exchange(&value, 0)

    member _.IsSetupCalled = setupCalled
    member _.IsCleanupCalled = cleanupCalled

type ParallelCounterStateWithSetup = {
    CurrentCount: Var<int>
}

/// Setup command - initializes counter to a specific value
type SetupCounterCommand() =
    inherit Command<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup, int, int>()

    override _.Name = "Setup"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, initialValue) =
        Task.FromResult(counter.Setup(initialValue))
    override _.Generate _ = Gen.int32 (Range.linearFrom 10 0 50)
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// Cleanup command - resets counter to zero
type CleanupCounterCommand() =
    inherit Command<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup, unit, int>()

    override _.Name = "Cleanup"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = Task.FromResult(counter.Cleanup())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

    override _.Ensure(_, _, _, _, result) =
        // After cleanup, value should be 0
        result = 0

/// Increment command
type ParallelIncrementWithSetupCommand() =
    inherit Command<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup, unit, int>()

    override _.Name = "Increment"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = Task.FromResult(counter.Increment())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// Decrement command
type ParallelDecrementWithSetupCommand() =
    inherit Command<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup, unit, int>()

    override _.Name = "Decrement"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = Task.FromResult(counter.Decrement())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// Get command
type ParallelGetWithSetupCommand() =
    inherit Command<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup, unit, int>()

    override _.Name = "Get"
    override _.Precondition _ = true
    override _.Execute(counter, _, _, _) = Task.FromResult(counter.Get())
    override _.Generate _ = Gen.constant ()
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }

/// ParallelSpecification with setup and cleanup
type ParallelCounterWithSetupSpec() =
    inherit ParallelSpecification<ThreadSafeCounterWithTracking, ParallelCounterStateWithSetup>()

    override _.InitialState = { CurrentCount = Var.symbolic 0 }
    override _.PrefixRange = Range.linear 0 3
    override _.BranchRange = Range.linear 1 5

    override _.SetupCommands = [|
        SetupCounterCommand()
    |]

    override _.Commands = [|
        ParallelIncrementWithSetupCommand()
        ParallelDecrementWithSetupCommand()
        ParallelGetWithSetupCommand()
    |]

    override _.CleanupCommands = [|
        CleanupCounterCommand()
    |]

[<Fact>]
let ``Parallel counter with setup and cleanup test``() =
    let sut = ThreadSafeCounterWithTracking()
    ParallelCounterWithSetupSpec().ToProperty(sut).Check()

    // Verify setup and cleanup were called
    Assert.True(sut.IsSetupCalled, "Setup should have been called")
    Assert.True(sut.IsCleanupCalled, "Cleanup should have been called")
