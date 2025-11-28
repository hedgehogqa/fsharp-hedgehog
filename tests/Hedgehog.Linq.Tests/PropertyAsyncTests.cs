using System;
using System.Threading.Tasks;
using Xunit;

namespace Hedgehog.Linq.Tests
{
    public class PropertyAsyncTests
    {
        [Fact]
        public void BlockingAsync_WorkCorrectly()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
                from y in Task.FromResult(x + 1)
                from z in Task.FromResult(y + 1)
                select z == x + 2;

            property.Check();
        }

        [Fact]
        public Task NonBlockingAsync_WorkCorrectly()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
                from y in Task.FromResult(x + 1)
                from z in Task.FromResult(y + 1)
                select z == x + 2;

            return property.CheckAsync();
        }

        [Fact]
        public Task Async_Property_CanReturn_TaskWithDelay()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
                from y in Task.Run(async () =>
                {
                    await Task.Delay(10);
                    return x + 1;
                })
                select y == x + 1;

            return property.CheckAsync();
        }

        [Fact]
        public async Task AsyncProperty_CanFail_WithTask()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
                from _ in Task.Run(async () =>
                {
                    await Task.Delay(10);
                    if (x > 50)
                    {
                        throw new Exception($"Value {x} is too large");
                    }
                })
                select true;

            var report = await property.ReportAsync();

            Assert.True(report.Status.IsFailed, "Expected property to fail");

            if (report.Status is Status.Failed failure)
            {
                Assert.True(failure.Item.Shrinks > 0, "Expected some shrinks");
            }
        }

        [Fact]
        public Task MultipleTaskBindings()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 50)).ForAll()
                from y in Task.FromResult(x * 2)
                from z in Task.Run(async () =>
                {
                    await Task.Delay(5);
                    return y + 10;
                })
                from w in Task.FromResult(z - x)
                select w == x + 10;

            return property.CheckAsync();
        }

        [Fact]
        public Task TaskWithGenBinding()
        {
            var property =
                from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
                from task in Task.FromResult(x > 50)
                from y in Gen.Int32(Range.Constant(0, 10)).ForAll()
                select task ? y >= 0 : y >= 0;

            return property.CheckAsync();
        }
    }
}
