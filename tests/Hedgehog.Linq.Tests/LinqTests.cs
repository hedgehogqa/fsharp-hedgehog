using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hedgehog.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public void ExceptionInSelect_Action_FailedStatus()
        {
            var guid = Guid.NewGuid().ToString();
            void action() => throw new Exception(guid);
            var property =
                from _ in Gen.Int32(Range.Constant(0, 0)).ForAll()
                select action();
            var report = property.Report();
            var rendered = report.Render();
            Assert.True(report.Status.IsFailed);
            Assert.Contains(guid, rendered);
        }

        [Fact]
        public void ExceptionInSelect_Func_FailedStatus()
        {
            var guid = Guid.NewGuid().ToString();
            bool func() => throw new Exception(guid);
            var property =
                from x in Gen.Int32(Range.Constant(0, 0)).ForAll()
                select func();
            var report = property.Report();
            var rendered = report.Render();
            Assert.True(report.Status.IsFailed);
            Assert.Contains(guid, rendered);
        }

        [Fact]
        public void RecheckOnlyTestsShrunkenInput()
        {
            var count = 0;
            var range = Range.Constant(0, 1000000);
            var gen = Gen.Int32(range);
            var prop =
                from i in gen.ForAll()
                let _ = count++
                select Assert.Equal(0, i);

            var report1 = prop.Report();
            if (report1.Status is Status.Failed failure1)
            {
                count = 0;
                var report2 = prop.ReportRecheck(failure1.Item.RecheckInfo.Value.Data);
                if (report2.Status is Status.Failed)
                {
                    Assert.Equal(1, count);
                } else
                {
                    throw new Exception("Recheck report should be Failed but is not");
                }
            } else
            {
                throw new Exception("Initial report should be Failed but is not");
            }
        }

        [Fact]
        // https://github.com/hedgehogqa/fsharp-hedgehog/issues/432
        public void RecheckIsFasterThanCheck()
        {
            var low = Gen.Int32(Range.Constant(0, 5));
            var mid = Gen.Int32(Range.Constant(10, 50));
            var big = Gen.Int32(Range.Constant(100, 200));
            var large = Gen.Int32(Range.Constant(500, 1000));
            var choice = Gen.Choice(low, mid, big, large).List(Range.Constant(100, 200));
            var prop = choice.ForAll().Select(x => x.Any(y => y == 990));
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();
            var report1 = prop.Report();
            watch.Stop();

            var checkTime = watch.ElapsedMilliseconds;
            watch.Reset();

            if (!(report1.Status is Status.Failed failure))
            {
                throw new Exception("Initial report should be Failed but is not");
            }

            watch.Start();
            prop.ReportRecheck(failure.Item.RecheckInfo.Value.Data);
            watch.Stop();

            var recheckTime = watch.ElapsedMilliseconds;
            Assert.InRange(recheckTime, 0, checkTime * 1.25); // Added 25% buffer for robustness
        }

        /*
         * The main object the following tests is just to make sure that the examples compile.
         * There's nothing fancy in the properties being tested.
         */

        [Fact]
        public void CanUseSelectWithAssertion()
        {
            var property =
                from x in Gen.Bool.ForAll()
                select Assert.True(x || !x);

            property.Check();
        }

        [Fact]
        public void CanUseSelectWithBool()
        {
            var property =
                from x in Gen.Bool.ForAll()
                select x || !x;

            property.Check();
        }

        [Fact]
        public void CanSelectFromTwoWithAssertion()
        {
            var property =
                from x in Gen.Bool.ForAll()
                from y in Gen.Bool.ForAll()
                select Assert.True((x || !x) && (y || !y));

            property.Check();
        }

        [Fact]
        public void CanSelectFromTwoWithBool()
        {
            var property =
                from x in Gen.Bool.ForAll()
                from y in Gen.Bool.ForAll()
                select (x || !x) && (y || !y);

            property.Check();
        }

        [Fact]
        public void CanSelectFromThreeWithAssertion()
        {
            var property =
                from x in Gen.Bool.ForAll()
                from y in Gen.Bool.ForAll()
                from z in Gen.Bool.ForAll()
                select Assert.True(x || y || z || (!x || !y || !z));

            property.Check();
        }

        [Fact]
        public void CanSelectFromThreeWithBool()
        {
            var property =
                from x in Gen.Bool.ForAll()
                from y in Gen.Bool.ForAll()
                from z in Gen.Bool.ForAll()
                select x || y || z || (!x || !y || !z);

            property.Check();
        }

        [Fact]
        public void CanUseWhereWithAssertion()
        {
            var property =
                from x in Gen.Constant(true).ForAll()
                where x == true
                from y in Gen.Constant(false).ForAll()
                where y == false
                select Assert.True(x && !y);

            property.Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanUseWhereWithBool()
        {
            var property =
                from x in Gen.Constant(true).ForAll()
                where x == true
                from y in Gen.Constant(false).ForAll()
                where y == false
                select x && !y;

            property.Check(PropertyConfig.Default.WithTests(20));
        }

        [Fact]
        public void CanDependOnEarlierValuesWithAssertion()
        {
            var property =
                from i in Gen.Int32(Range.Constant(1, 10)).ForAll()
                from j in Gen.Int32(Range.Constant(1, i)).ForAll()
                select Assert.True(j <= i);

            property.Check();
        }

        [Fact]
        public void CanDependOnEarlierValuesWithBool()
        {
            var property =
                from i in Gen.Int32(Range.Constant(1, 10)).ForAll()
                from j in Gen.Int32(Range.Constant(1, i)).ForAll()
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
            Gen.Frequency(
                (1, Gen.Int32(Range.Constant(0, 100))),
                (1, Gen.Int32(Range.Constant(0, 100)))
                );
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

        [Fact]
        public void SubsetOf_Should_Only_Contain_Elements_From_Original_Set()
        {
            var prop =
                from originalItems in Gen.Int32(Range.ExponentialBoundedInt32()).List(Range.LinearInt32(0, 50)).ForAll()
                from subset in Gen.SubsetOf(originalItems).ForAll()
                let originalSet = originalItems.ToHashSet()
                select subset.All(originalSet.Contains);

            prop.Check();
        }

        [Fact]
        public void SubsetOf_Should_Preserve_Original_Element_Order()
        {
            var prop =
                from originalItems in Gen.Int32(Range.ExponentialBoundedInt32()).List(Range.LinearInt32(0, 50)).ForAll()
                from subset in Gen.SubsetOf(originalItems).ForAll()
                let subsetList = subset.ToList()
                let inOrder = IsInOrder(originalItems, subsetList)
                select inOrder;

            prop.Check();
        }

        private static bool IsInOrder<T>(IList<T> original, IList<T> subset)
        {
            var subsetIndex = 0;
            foreach (var item in original)
            {
                if (subsetIndex >= subset.Count) break;
                if (EqualityComparer<T>.Default.Equals(item, subset[subsetIndex])) subsetIndex++;
            }

            return subsetIndex == subset.Count;
        }

        [Fact]
        public void SubsetOf_Should_Not_Introduce_Duplicate_Elements()
        {
            var prop =
                from originalItems in Gen.Int32(Range.ExponentialBoundedInt32()).List(Range.LinearInt32(0, 50)).ForAll()
                let distinctOriginal = originalItems.Distinct().ToList()
                from subset in Gen.SubsetOf(distinctOriginal).ForAll()
                let subsetList = subset.ToList()
                select subsetList.Count == subsetList.Distinct().Count();

            prop.Check();
        }
    }
}
