using Hedgehog.Stateful.Linq;

namespace Hedgehog.Stateful.Tests.CSharp;

using Hedgehog.Linq;
using Xunit;

/// <summary>
/// Simple counter for demonstrating sequential state machine testing
/// </summary>
public class Counter
{
    private int _value;

    public void Increment() => _value++;

    public void Decrement() => _value--;

    public int AddRandom()
    {
        var rnd = new Random().Next(100);
        _value += rnd;
        return _value;
    }

    public void Set(int n) => _value = n;

    public int Get() => _value;

    public void Reset() => _value = 0;
}

/// <summary>
/// Model state tracking the current count symbolically
/// </summary>
public record CounterState
{
    public required Var<int> CurrentCount { get; init; }
}

/// <summary>
/// Increment command - increments the counter and returns new value
/// </summary>
public class IncrementCommand : Command<Counter, CounterState, NoValue, int>
{
    public override string Name => "Increment";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, NoValue value) => Precondition(state);

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoValue value)
    {
        sut.Increment();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<NoValue> Generate(CounterState state) =>
        Gen.Constant(NoValue.Value);

    public override CounterState Update(CounterState state, NoValue value, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoValue value, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount + 1;
    }
}

/// <summary>
/// AddRandom command - adds a random value to the counter
/// </summary>
public class AddRandomCommand : Command<Counter, CounterState, NoValue, int>
{
    public override string Name => "AddRandom";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, NoValue value) => Precondition(state);

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoValue value) =>
        Task.FromResult(sut.AddRandom());

    public override Gen<NoValue> Generate(CounterState state) =>
        Gen.Constant(NoValue.Value);


    public override CounterState Update(CounterState state, NoValue value, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoValue value, int output) => true;
}

/// <summary>
/// Decrement command - decrements the counter and returns new value
/// </summary>
public class DecrementCommand : Command<Counter, CounterState, NoValue, int>
{
    public override string Name => "Decrement";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, NoValue value) => Precondition(state);

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoValue value)
    {
        sut.Decrement();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<NoValue> Generate(CounterState state) =>
        Gen.Constant(NoValue.Value);


    public override CounterState Update(CounterState state, NoValue value, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoValue value, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount - 1;
    }
}

/// <summary>
/// Reset command - resets the counter to 0
/// </summary>
public class ResetCommand : Command<Counter, CounterState, NoValue, int>
{
    public override string Name => "Reset";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, NoValue value) => Precondition(state);

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoValue value)
    {
        sut.Reset();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<NoValue> Generate(CounterState state) =>
        Gen.Constant(NoValue.Value);


    public override CounterState Update(CounterState state, NoValue value, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoValue value, int result)
    {
        // Reset always sets to 0
        return result == 0;
    }
}

/// <summary>
/// Set command - sets the counter to a specific value
/// </summary>
public class SetCommand : Command<Counter, CounterState, int, int>
{
    public override string Name => "Set";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, int input) => true;

    public override Task<int> Execute(Counter sut, Env env, CounterState state, int value)
    {
        sut.Set(value);
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<int> Generate(CounterState state) =>
        Gen.Int32(Range.LinearFromInt32(0, -10, 100));

    public override CounterState Update(CounterState state, int input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, int input, int result)
    {
        Assert.Equal(input, result);
        return result == input;
    }
}

/// <summary>
/// Get command - returns the current counter value
/// </summary>
public class GetCommand : Command<Counter, CounterState, NoValue, int>
{
    public override string Name => "Get";

    public override bool Precondition(CounterState state) => true;
    public override bool Require(Env env, CounterState state, NoValue value) => Precondition(state);

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoValue value) =>
        Task.FromResult(sut.Get());

    public override Gen<NoValue> Generate(CounterState state) =>
        Gen.Constant(NoValue.Value);

    public override CounterState Update(CounterState state, NoValue value, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoValue value, int result)
    {
        return result == oldState.CurrentCount.Resolve(env);
    }
}

/// <summary>
/// Sequential specification that manages Counter lifecycle
/// </summary>
public class CounterSpec : SequentialSpecification<Counter, CounterState>
{
    public override ICommand<Counter, CounterState>[] SetupCommands => [new ResetCommand()];

    public override ICommand<Counter, CounterState>[] CleanupCommands => [
        new ResetCommand()
    ];

    public override CounterState InitialState => new() { CurrentCount = Var.Symbolic(0) };

    public override Range<int> Range => Hedgehog.Linq.Range.LinearInt32(1, 50);

    public override ICommand<Counter, CounterState>[] Commands =>
        [
            new IncrementCommand(),
            new DecrementCommand(),
            new ResetCommand(),
            new GetCommand(),
            new SetCommand(),
            new AddRandomCommand()
        ];
}

public class SequentialCounterTests
{
    [Fact]
    public void CounterTest()
    {
        var sut = new Counter();
        new CounterSpec().ToProperty(sut).Check();
    }
}
