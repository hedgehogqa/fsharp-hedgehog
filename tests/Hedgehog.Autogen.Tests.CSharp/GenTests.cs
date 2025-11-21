using Xunit;
using AwesomeAssertions;
using Hedgehog.Linq;
using Range = Hedgehog.Linq.Range;

namespace Hedgehog.AutoGen.Linq.Tests;

public sealed record NameAge(string Name, int Age);

public class GenTests
{
  [Fact]
  public void ShouldUseGenConfig()
  {
    var onlyString = "singleton string";
    var config = AutoGenConfig.Defaults.AddGenerator(Gen.Constant(onlyString));

    var list = Gen.AutoWith<NameAge>(config).Sample(123, 3);

    list.Should().HaveCount(3);
    _ = list.Should().AllSatisfy(x => x.Name.Should().Be(onlyString));
  }

  [Fact]
  public void ShouldGenerateWithNull()
  {
    var values = Gen.Constant("a").WithNull().Sample(1, 1000);
    _ = values.Should().Contain(x => x == null);
  }

  [Fact]
  public void ShouldNotGenerateWithNull()
  {
    var values = Gen
      .Constant("a")
      .WithNull()
      .NotNull()
      .Sample(1, 1000);
    _ = values.Should().NotContainNulls();
  }

  [Fact]
  public void ShouldAddElementToList()
  {
    var prop =
      from x in Gen.Int32(Range.ExponentialBoundedInt32()).ForAll()
      from xs in Gen
        .Int32(Range.ExponentialBoundedInt32())
        .List(Range.LinearInt32(0, 10))
        .AddElement(x)
        .ForAll()
      select xs.Contains(x);

    prop.Check();
  }

  [Fact]
  public void ShouldSupportIEnumerable() =>
    Gen.Auto<IEnumerable<int>>()
      .Sample(1, 5)
      .Should()
      .AllSatisfy(x => x.Should().NotBeNull());
}
