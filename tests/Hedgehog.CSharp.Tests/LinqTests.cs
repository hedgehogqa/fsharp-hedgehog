using Xunit;

// Import Check and ForAll:
using static Hedgehog.Property;

namespace Hedgehog.CSharp.Tests
{
    /*
     * The main object here is just to make sure that the examples compile,
     * there's nothing fancy in the properties being tested.
     */
    public class LinqTests
    {
        [Fact]
        public void CanUseSelectWithAssertion()
        {
            Check(
                from x in ForAll(Gen.Bool)
                select Assert.True(x || !x));
        }

        [Fact]
        public void CanUseSelectWithBool()
        {
            Check(
                from x in ForAll(Gen.Bool)
                select x || !x);
        }

        [Fact]
        public void CanSelectFromTwoWithAssertion()
        {
            Check(
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                select Assert.True((x || !x) && (y || !y)));
        }

        [Fact]
        public void CanSelectFromTwoWithBool()
        {
            Check(
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                select (x || !x) && (y || !y));
        }

        [Fact]
        public void CanSelectFromThreeWithAssertion()
        {
            Check(
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                from z in ForAll(Gen.Bool)
                select Assert.True(x || y || z || (!x || !y || !z)));
        }

        [Fact]
        public void CanSelectFromThreeWithBool()
        {
            Check(
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                from z in ForAll(Gen.Bool)
                select x || y || z || (!x || !y || !z));
        }

        [Fact]
        public void CanUseWhereWithAssertion()
        {
            Check(20,
                from x in ForAll(Gen.Bool)
                where x == true
                from y in ForAll(Gen.Bool)
                where y == false
                select Assert.True(x && !y));
        }

        [Fact]
        public void CanUseWhereWithBool()
        {
            Check(20,
                from x in ForAll(Gen.Bool)
                where x == true
                from y in ForAll(Gen.Bool)
                where y == false
                select x && !y);
        }

        [Fact]
        public void CanDependOnEarlierValuesWithAssertion()
        {
            Check(
                from i in ForAll(Gen.Int32(Range.Constant(1, 10)))
                from j in ForAll(Gen.Int32(Range.Constant(1, i)))
                select Assert.True(j <= i));
        }

        [Fact]
        public void CanDependOnEarlierValuesWithBool()
        {
            Check(
                from i in ForAll(Gen.Int32(Range.Constant(1, 10)))
                from j in ForAll(Gen.Int32(Range.Constant(1, i)))
                select j <= i);
        }
    }
}
