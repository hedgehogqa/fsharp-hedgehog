namespace Hedgehog.Xunit.Tests.CSharp;

public class Async
{
    internal static Task FooAsync() => Task.Delay(100);

    [Property]
    public async Task Async_property_which_returns_task_can_run(
      int i)
    {
        await FooAsync();
        Assert.StrictEqual(i, i);
    }

    [Property]
    public async Task<bool> Async_property_which_returns_boolean_task_can_run(
        bool i)
    {
        await FooAsync();
        return i || !i;
    }
}
