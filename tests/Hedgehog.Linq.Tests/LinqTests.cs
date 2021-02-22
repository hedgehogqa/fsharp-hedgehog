using System;
using Xunit;

// Import ForAll:
using static Hedgehog.Linq.Property;

namespace Hedgehog.Linq.Tests
{
    public class LinqTests {

        [Fact]
        public void ExceptionInSelect_Action_FailedStatus() {
            void action() => throw new Exception();
            var property =
                from _ in Property.ForAll(Gen.Int32(Range.Constant(0, 0)))
                select action();
            var report = property.Report();
            Assert.True(report.Status.IsFailed);
        }

        [Fact]
        public void ExceptionInSelect_Func_FailedStatus() {
            bool func() => throw new Exception();
            var property =
                from x in Property.ForAll(Gen.Int32(Range.Constant(0, 0)))
                select func();
            var report = property.Report();
            Assert.True(report.Status.IsFailed);
        }

        /*
         * The main object the following tests is just to make sure that the examples compile.
         * There's nothing fancy in the properties being tested.
         */

        [Fact]
        public void CanUseSelectWithAssertion()
        {
            var property =
                from x in ForAll(Gen.Bool)
                select Assert.True(x || !x);

            property.Check();
        }

        [Fact]
        public void CanUseSelectWithBool()
        {
            var property =
                from x in ForAll(Gen.Bool)
                select x || !x;

            property.Check();
        }

        [Fact]
        public void CanSelectFromTwoWithAssertion()
        {
            var property =
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                select Assert.True((x || !x) && (y || !y));

            property.Check();
        }

        [Fact]
        public void CanSelectFromTwoWithBool()
        {
            var property =
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                select (x || !x) && (y || !y);

            property.Check();
        }

        [Fact]
        public void CanSelectFromThreeWithAssertion()
        {
            var property =
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                from z in ForAll(Gen.Bool)
                select Assert.True(x || y || z || (!x || !y || !z));

            property.Check();
        }

        [Fact]
        public void CanSelectFromThreeWithBool()
        {
            var property =
                from x in ForAll(Gen.Bool)
                from y in ForAll(Gen.Bool)
                from z in ForAll(Gen.Bool)
                select x || y || z || (!x || !y || !z);

            property.Check();
        }

        [Fact]
        public void CanUseWhereWithAssertion()
        {
            var property =
                from x in ForAll(Gen.FromValue(true))
                where x == true
                from y in ForAll(Gen.FromValue(false))
                where y == false
                select Assert.True(x && !y);

            property.Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanUseWhereWithBool()
        {
            var property =
                from x in ForAll(Gen.FromValue(true))
                where x == true
                from y in ForAll(Gen.FromValue(false))
                where y == false
                select x && !y;

            property.Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanDependOnEarlierValuesWithAssertion()
        {
            var property =
                from i in ForAll(Gen.Int32(Range.Constant(1, 10)))
                from j in ForAll(Gen.Int32(Range.Constant(1, i)))
                select Assert.True(j <= i);

            property.Check();
        }

        [Fact]
        public void CanDependOnEarlierValuesWithBool()
        {
            var property =
                from i in ForAll(Gen.Int32(Range.Constant(1, 10)))
                from j in ForAll(Gen.Int32(Range.Constant(1, i)))
                select j <= i;

            property.Check();
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
