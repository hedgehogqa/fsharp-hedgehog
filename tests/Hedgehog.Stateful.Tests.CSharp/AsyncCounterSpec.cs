namespace Hedgehog.Stateful.Tests.CSharp;

using Hedgehog.Linq;
using Xunit;

/// <summary>
/// Async Counter - all operations return Tasks
/// </summary>
public class AsyncCounter
{
    private int _value;

    public async Task IncrementAsync()
    {
        await Task.Delay(1); // Simulate async work
        _value++;
    }

    public async Task DecrementAsync()
    {
        await Task.Delay(1);
        _value--;
    }

    public async Task<int> AddRandomAsync()
    {
        await Task.Delay(1);
        var rnd = new Random().Next(100);
        _value += rnd;
        return _value;
    }

    public async Task SetAsync(int n)
    {
        await Task.Delay(1);
        _value = n;
    }

    public async Task<int> GetAsync()
    {
        await Task.Delay(1);
        return _value;
    }

    public async Task ResetAsync()
    {
        await Task.Delay(1);
        _value = 0;
    }
}

/// <summary>
/// Model state for async counter
/// </summary>
public record AsyncCounterState
{
    public required Var<int> CurrentCount { get; init; }
}

/// <summary>
/// Async increment command
/// </summary>
public class AsyncIncrementCommand : Command<AsyncCounter, AsyncCounterState, bool, int>
{
    public override string Name => "IncrementAsync";

    public override bool Precondition(AsyncCounterState state) => true;
    public override bool Require(Env env, AsyncCounterState state, bool input) => Precondition(state);

    public override async Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, bool input)
    {
        await sut.IncrementAsync();
        return await sut.GetAsync();
    }

    public override Gen<bool> Generate(AsyncCounterState state) =>
        Gen.Constant(true);


    public override AsyncCounterState Update(AsyncCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, bool input, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount + 1;
    }
}

/// <summary>
/// Async decrement command
/// </summary>
public class AsyncDecrementCommand : Command<AsyncCounter, AsyncCounterState, bool, int>
{
    public override string Name => "DecrementAsync";

    public override bool Precondition(AsyncCounterState state) => true;
    public override bool Require(Env env, AsyncCounterState state, bool input) => Precondition(state);

    public override async Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, bool input)
    {
        await sut.DecrementAsync();
        return await sut.GetAsync();
    }

    public override Gen<bool> Generate(AsyncCounterState state) =>
        Hedgehog.Linq.Gen.Constant(true);


    public override AsyncCounterState Update(AsyncCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, bool input, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount - 1;
    }
}

/// <summary>
/// Async reset command
/// </summary>
public class AsyncResetCommand : Command<AsyncCounter, AsyncCounterState, bool, int>
{
    public override string Name => "ResetAsync";

    public override bool Precondition(AsyncCounterState state) => true;
    public override bool Require(Env env, AsyncCounterState state, bool input) => Precondition(state);

    public override async Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, bool input)
    {
        await sut.ResetAsync();
        return await sut.GetAsync();
    }

    public override Gen<bool> Generate(AsyncCounterState state) =>
        Gen.Constant(true);


    public override AsyncCounterState Update(AsyncCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, bool input, int result)
    {
        return result == 0;
    }
}

/// <summary>
/// Async set command
/// </summary>
public class AsyncSetCommand : Command<AsyncCounter, AsyncCounterState, int, int>
{
    public override string Name => "SetAsync";

    public override bool Precondition(AsyncCounterState state) => true;

    public override bool Require(Env env, AsyncCounterState state, int input) => Precondition(state);

    public override async Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, int value)
    {
        await sut.SetAsync(value);
        return await sut.GetAsync();
    }

    public override Gen<int> Generate(AsyncCounterState state) =>
        Gen.Int32(Range.LinearFromInt32(0, -10, 100));


    public override AsyncCounterState Update(AsyncCounterState state, int input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, int input, int result)
    {
        return result == input;
    }
}

/// <summary>
/// Async get command
/// </summary>
public class AsyncGetCommand : Command<AsyncCounter, AsyncCounterState, bool, int>
{
    public override string Name => "GetAsync";

    public override bool Precondition(AsyncCounterState state) => true;
    public override bool Require(Env env, AsyncCounterState state, bool input) => Precondition(state);

    public override Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, bool input) =>
        sut.GetAsync();

    public override Gen<bool> Generate(AsyncCounterState state) =>
        Gen.Constant(true);


    public override AsyncCounterState Update(AsyncCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, bool input, int result)
    {
        return result == oldState.CurrentCount.Resolve(env);
    }
}

/// <summary>
/// Async AddRandom command
/// </summary>
public class AsyncAddRandomCommand : Command<AsyncCounter, AsyncCounterState, bool, int>
{
    public override string Name => "AddRandomAsync";

    public override bool Precondition(AsyncCounterState state) => true;
    public override bool Require(Env env, AsyncCounterState state, bool input) => Precondition(state);

    public override Task<int> Execute(AsyncCounter sut, Env env, AsyncCounterState state, bool input) =>
        sut.AddRandomAsync();

    public override Gen<bool> Generate(AsyncCounterState state) =>
        Gen.Constant(true);

    public override bool Ensure(Env env, AsyncCounterState oldState, AsyncCounterState newState, bool input, int output) => true;

    public override AsyncCounterState Update(AsyncCounterState state, bool input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };
}

/// <summary>
/// Async Counter Specification
/// </summary>
public class AsyncCounterSpec : SequentialSpecification<AsyncCounter, AsyncCounterState>
{
    public override ICommand<AsyncCounter, AsyncCounterState>[] SetupCommands =>
        [new AsyncResetCommand()];

    public override ICommand<AsyncCounter, AsyncCounterState>[] CleanupCommands => [];

    public override AsyncCounterState InitialState => new() { CurrentCount = Var.Symbolic(0) };

    public override Range<int> Range => Hedgehog.Linq.Range.LinearInt32(1, 50);

    public override ICommand<AsyncCounter, AsyncCounterState>[] Commands =>
        [
            new AsyncIncrementCommand(),
            new AsyncDecrementCommand(),
            new AsyncResetCommand(),
            new AsyncGetCommand(),
            new AsyncSetCommand(),
            new AsyncAddRandomCommand()
        ];
}

public class AsyncCounterTests
{
    [Fact]
    public void AsyncCounterTest()
    {
        var sut = new AsyncCounter();
        new AsyncCounterSpec().ToProperty(sut).Check();
    }
}
