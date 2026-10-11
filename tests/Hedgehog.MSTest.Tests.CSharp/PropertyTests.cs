using AwesomeAssertions;
using Hedgehog.Linq;

namespace Hedgehog.MSTest.Tests.CSharp;

public static class Int13
{
    public static IAutoGenConfig Config => AutoGenConfig.Defaults.AddGenerator(Gen.Constant(13));
}

public static class ConstantInt
{
    public static IAutoGenConfig Config(int value) => AutoGenConfig.Defaults.AddGenerator(Gen.Constant(value));
}

public sealed class Int5Attribute : GenAttribute<int>
{
    public override Gen<int> Generator => Gen.Constant(5);
}

[TestClass]
public sealed class PropertyTests
{
    private static int _casesOfTestsSetting;
    private static int _casesOfTestsArgument;
    private static int _casesOfRecheck;

    [ClassCleanup]
    public static void Settings_Should_Have_Decided_The_Number_Of_Cases()
    {
        _casesOfTestsSetting.Should().Be(7, "Tests = 7 runs seven cases");
        _casesOfTestsArgument.Should().Be(3, "the tests argument of the constructor runs three cases");
        _casesOfRecheck.Should().Be(1, "a recheck replays exactly one case");
    }

    [Property]
    public void Property_Should_Generate_Its_Arguments(int number, string text, List<bool> flags)
    {
        text.Should().NotBeNull();
        flags.Should().NotBeNull();
        number.Should().Be(number);
    }

    [Property(Tests = 7)]
    public void Tests_Setting_Should_Decide_The_Number_Of_Cases(int _) =>
        Interlocked.Increment(ref _casesOfTestsSetting);

    [Property(3)]
    public void Tests_Argument_Should_Decide_The_Number_Of_Cases(int _) =>
        Interlocked.Increment(ref _casesOfTestsArgument);

    [Property(Tests = 5, Shrinks = 0, Size = 1, Seed = 42UL)]
    public void Named_Settings_Should_Be_Accepted_Together(int value) =>
        value.Should().Be(value);

    [Property(AutoGenConfig = typeof(Int13))]
    public void AutoGenConfig_Setting_Should_Supply_The_Generator(int value) =>
        value.Should().Be(13);

    [Property(typeof(Int13), 5)]
    public void AutoGenConfig_Argument_Should_Supply_The_Generator(int value) =>
        value.Should().Be(13);

    [Property(AutoGenConfig = typeof(ConstantInt), AutoGenConfigArgs = new object[] { 21 })]
    public void AutoGenConfigArgs_Setting_Should_Be_Passed_To_The_Config(int value) =>
        value.Should().Be(21);

    [Property]
    public void GenAttribute_Of_The_Test_Project_Should_Supply_The_Generator([Int5] int value) =>
        value.Should().Be(5);

    [Property(AutoGenConfig = typeof(Int13))]
    public void GenAttribute_Should_Win_Over_AutoGenConfig([Int5] int fromAttribute, int fromConfig)
    {
        fromAttribute.Should().Be(5);
        fromConfig.Should().Be(13);
    }

    [Property(Tests = 5)]
    [DataRow(7)]
    [DataRow(8)]
    public void DataRow_Should_Come_Before_The_Generated_Arguments(int row, [Int5] int generated)
    {
        row.Should().BeOneOf(7, 8);
        generated.Should().Be(5);
    }

    [Property]
    [Recheck("1_9056294896546497174_14632957226901407867_")]
    public void Recheck_Should_Replay_One_Case(int _) =>
        Interlocked.Increment(ref _casesOfRecheck);
}

[TestClass]
[Properties(AutoGenConfig = typeof(Int13), Tests = 5)]
public sealed class PropertiesTests
{
    [Property]
    public void Properties_Should_Supply_The_Generator_Of_Every_Property(int value) =>
        value.Should().Be(13);

    [Property(AutoGenConfig = typeof(ConstantInt), AutoGenConfigArgs = new object[] { 21 })]
    public void Property_Setting_Should_Win_Over_Properties(int value) =>
        value.Should().Be(21);
}

[TestClass]
public sealed class AsyncTests
{
    [Property]
    public async Task Async_Property_Which_Returns_Task_Should_Run(int value)
    {
        await Task.Delay(1);
        value.Should().Be(value);
    }

    [Property]
    public async ValueTask Async_Property_Which_Returns_ValueTask_Should_Run(bool value)
    {
        await Task.Yield();
        (value || !value).Should().BeTrue();
    }
}
