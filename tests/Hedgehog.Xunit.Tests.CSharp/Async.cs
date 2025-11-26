namespace Hedgehog.Xunit.Tests.CSharp;

public class Async
{
    public sealed record TestValue(string Value);
    internal static Task FooAsync() => Task.Delay(1);

    [Property]
    public async Task Async_property_which_returns_task_can_run(int i)
    {
        await FooAsync();
        Assert.StrictEqual(i, i);
    }

    [Property]
    public async Task<bool> Async_property_which_returns_boolean_task_can_run(bool i)
    {
        await FooAsync();
        return i || !i;
    }

    [Property]
    public async ValueTask<bool> Async_property_with_value_task_can_run(bool i)
    {
        return await ValueTask.FromResult(i || !i);
    }

    [Property]
    public async Task<TestValue> Async_property_with_custom_value_should_run(string s)
    {
        var result = await Task.FromResult(new TestValue(s));
        Assert.Equal(s, result.Value);
        return result;
    }

    [Property]
    public async ValueTask<TestValue> Async_property_returning_ValueTask_with_custom_type(string s)
    {
        await Task.Delay(1);
        var result = new TestValue(s);
        Assert.Equal(s, result.Value);
        return result;
    }

    public void ThisFunctionThrows()
    {
        throw new InvalidOperationException("Boo");
    }

    [Property]
    public bool Test(string myString, int myInt, TestValue myValue)
    {
        return !myString.Contains("c");
    }

    [Property]
    public void ThrowsTest(string myString, int myInt, TestValue myValue)
    {
        if (myString.Contains("c")) ThisFunctionThrows();
    }
}
