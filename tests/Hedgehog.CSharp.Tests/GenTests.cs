using Xunit;

using static Hedgehog.Property;

namespace Hedgehog.CSharp.Tests
{
    public class GenTests
    {
        [Fact]
        public void CanUseMapInCSharpFriendlyManner()
        {
            Gen<int> gen =
                Gen.Map(x => x + 1, Gen.Constant(1));
            Check(
                from x in ForAll(gen)
                select Assert.Equal(2, x));
        }
    }
}