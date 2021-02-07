using Xunit;

namespace Hedgehog.Linq.Tests
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
            var property =
                from x in Gen.Bool
                select Assert.True(x || !x);

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanUseSelectWithBool()
        {
            var property =
                from x in Gen.Bool
                select x || !x;

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanSelectFromTwoWithAssertion()
        {
            var property =
                from x in Gen.Bool
                from y in Gen.Bool
                select Assert.True((x || !x) && (y || !y));

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanSelectFromTwoWithBool()
        {
            var property =
                from x in Gen.Bool
                from y in Gen.Bool
                select (x || !x) && (y || !y);

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanSelectFromThreeWithAssertion()
        {
            var property =
                from x in Gen.Bool
                from y in Gen.Bool
                from z in Gen.Bool
                select Assert.True(x || y || z || (!x || !y || !z));

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanSelectFromThreeWithBool()
        {
            var property =
                from x in Gen.Bool
                from y in Gen.Bool
                from z in Gen.Bool
                select x || y || z || (!x || !y || !z);

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanUseWhereWithAssertion()
        {
            var property =
                from x in Gen.FromValue(true)
                where x == true
                from y in Gen.FromValue(false)
                where y == false
                select Assert.True(x && !y);

            Property.ForAll(property).Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanUseWhereWithBool()
        {
            var property =
                from x in Gen.FromValue(true)
                where x == true
                from y in Gen.FromValue(false)
                where y == false
                select x && !y;

            Property.ForAll(property).Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanDependOnEarlierValuesWithAssertion()
        {
            var property =
                from i in Gen.Int32(Range.Constant(1, 10))
                from j in Gen.Int32(Range.Constant(1, i))
                select Assert.True(j <= i);

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanDependOnEarlierValuesWithBool()
        {
            var property =
                from i in Gen.Int32(Range.Constant(1, 10))
                from j in Gen.Int32(Range.Constant(1, i))
                select j <= i;

            Property.ForAll(property).Check();
        }

        [Fact]
        public void CanUseSelectWithGen()
        {
            Gen<bool> a =
                from i in Gen.Bool
                select !i;
        }

        [Fact]
        public void CanUseWhereWithGen()
        {
            Gen<bool> a =
                from i in Gen.Bool
                where i
                select !i;
        }

        [Fact]
        public void CanUseSelectManyWithGen()
        {
            Gen<bool> a =
                from i in Gen.Bool
                from j in Gen.Bool
                select !i;
        }

        [Fact]
        public void CanUseSelectWithRange()
        {
            Range<int> a =
                from i in Range.Constant(1, 10)
                select i + 10;
        }
    }
}
