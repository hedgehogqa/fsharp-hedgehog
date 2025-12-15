namespace Hedgehog.Stateful.Tests.CSharp;

using Hedgehog.Linq;
using Xunit;

/*
    PARALLEL TESTING: Testing Linearizability of Concurrent Operations

    This example demonstrates how parallel state machine testing works.

    WHAT DOES IT TEST?
    -------------------
    Parallel testing verifies that a concurrent data structure (like our ThreadSafeCounter)
    is "linearizable" - meaning that despite operations running in parallel, the observed
    results match what could happen if those operations executed in SOME sequential order.

    HOW DOES IT WORK?
    -----------------
    1. SEQUENTIAL PREFIX: First, a sequence of operations runs sequentially to set up
       some initial state (e.g., counter might be at value 5).

    2. PARALLEL BRANCHES: Then, TWO branches of operations run IN PARALLEL on the SAME SUT.
       For example:
         Thread 1: Increment, Get, Increment
         Thread 2: Decrement, Get
       These run concurrently - they WILL interfere with each other!

    3. LINEARIZABILITY CHECK: After parallel execution, the framework checks if there exists
       ANY valid sequential ordering of all operations that would produce the same results.

       If Branch 1 observed: [6, 6, 7] and Branch 2 observed: [5, 5], the framework checks:
       - Could Increment, Decrement, Get, Increment, Get produce these results?
       - Could Decrement, Increment, Get, Get, Increment produce these results?
       - ... checks all possible interleavings ...

       If ANY interleaving matches, the test passes (operations are linearizable).
       If NO interleaving matches, the test FAILS (there's a concurrency bug).

    WHY DOES THIS WORK?
    -------------------
    The key insight: The same SUT is INTENTIONALLY shared between parallel branches.
    This is not a bug - it's the whole point! We WANT operations to interfere, because
    we're testing whether the SUT can handle concurrent access correctly.

    If the SUT is correctly thread-safe (like ThreadSafeCounter with Interlocked operations),
    all observed results will be linearizable.

    If the SUT has concurrency bugs (e.g., unsynchronized read-modify-write), some test
    runs will observe results that can't be explained by ANY sequential ordering, and the
    test will fail.
*/

/// <summary>
/// Thread-safe counter for parallel testing using Interlocked operations
/// This tests whether concurrent operations are linearizable
/// </summary>
public class ThreadSafeCounter
{
    private int _value;

    public int Increment() => Interlocked.Increment(ref _value);

    public int Decrement() => Interlocked.Decrement(ref _value);

    public int Get() => Interlocked.CompareExchange(ref _value, 0, 0);
}

/// <summary>
/// Model state for parallel counter testing
/// </summary>
public record ParallelCounterState
{
    public required Var<int> CurrentCount { get; init; }
}

/// <summary>
/// Parallel increment command - returns the new value after incrementing
/// </summary>
public class ParallelIncrementCommand : Command<ThreadSafeCounter, ParallelCounterState, bool, int>
{
    public override string Name => "Increment";
    public override bool Precondition(ParallelCounterState state) => true;
    public override bool Require(Env env, ParallelCounterState state, bool input) => Precondition(state);

    public override Task<int> Execute(ThreadSafeCounter sut, Env env, ParallelCounterState state, bool input) =>
        Task.FromResult(sut.Increment());

    public override Gen<bool> Generate(ParallelCounterState state) =>
        Gen.Constant(true);



    public override ParallelCounterState Update(ParallelCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, ParallelCounterState oldState, ParallelCounterState newState, bool input, int result)
    {
        // For parallel testing, Ensure should only check weak postconditions
        // The framework will verify linearizability by checking all interleavings
        // We can only check that the result is a valid counter value
        return result > 0; // After increment, value should be positive (assuming we start at 0)
    }
}

/// <summary>
/// Parallel decrement command - returns the new value after decrementing
/// </summary>
public class ParallelDecrementCommand : Command<ThreadSafeCounter, ParallelCounterState, bool, int>
{
    public override string Name => "Decrement";

    public override bool Precondition(ParallelCounterState state) => true;
    public override bool Require(Env env, ParallelCounterState state, bool input) => Precondition(state);

    public override Task<int> Execute(ThreadSafeCounter sut, Env env, ParallelCounterState state, bool input) =>
        Task.FromResult(sut.Decrement());

    public override Gen<bool> Generate(ParallelCounterState state) =>
        Gen.Constant(true);


    public override ParallelCounterState Update(ParallelCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, ParallelCounterState oldState, ParallelCounterState newState, bool input, int output) => true;
}

/// <summary>
/// Parallel get command - reads the current value
/// </summary>
public class ParallelGetCommand : Command<ThreadSafeCounter, ParallelCounterState, bool, int>
{
    public override string Name => "Get";

    public override bool Precondition(ParallelCounterState state) => true;
    public override bool Require(Env env, ParallelCounterState state, bool input) => Precondition(state);

    public override Task<int> Execute(ThreadSafeCounter sut, Env env, ParallelCounterState state, bool input) =>
        Task.FromResult(sut.Get());

    public override Gen<bool> Generate(ParallelCounterState state) =>
        Gen.Constant(true);


    public override ParallelCounterState Update(ParallelCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, ParallelCounterState oldState, ParallelCounterState newState, bool input, int output) => true;
}

/// <summary>
/// Parallel specification for testing linearizability of ThreadSafeCounter
/// </summary>
public class ParallelCounterSpec : ParallelSpecification<ThreadSafeCounter, ParallelCounterState>
{
    public override ParallelCounterState InitialState => new() { CurrentCount = Var.Symbolic(0) };

    // Generate prefix sequences of 0-3 actions (setup some initial state)
    public override Range<int> PrefixRange => Range.LinearInt32(0, 3);

    // Generate branch sequences of 1-5 actions per thread
    public override Range<int> BranchRange => Range.LinearInt32(1, 5);

    // Commands that can run in parallel
    public override ICommand<ThreadSafeCounter, ParallelCounterState>[] Commands =>
        [
            new ParallelIncrementCommand(),
            new ParallelDecrementCommand(),
            new ParallelGetCommand()
        ];

    public override ICommand<ThreadSafeCounter, ParallelCounterState>[] SetupCommands => [];
    public override ICommand<ThreadSafeCounter, ParallelCounterState>[] CleanupCommands => [];
}

public class ParallelCounterTests
{
    [Fact]
    public void ParallelCounterTest()
    {
        var sut = new ThreadSafeCounter();
        // This will:
        // 1. Run a sequential prefix to set up initial state
        // 2. Run two branches in parallel
        // 3. Verify that the results are linearizable (match some sequential interleaving)
        new ParallelCounterSpec().ToProperty(sut).Check();
    }
}
