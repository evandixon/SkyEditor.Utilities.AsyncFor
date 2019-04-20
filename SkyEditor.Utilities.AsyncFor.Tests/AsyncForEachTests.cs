using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SkyEditor.Utilities.AsyncFor.Tests
{
    public class AsyncForEachTests
    {
        [Fact]
        public async Task RunsForEveryItemInCollectionWithSynchronousDelegate_StaticMethod()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            await sampleData.RunAsyncForEach(data =>
            {
                data.Success = true;
            });

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task RunsForEveryItemInCollectionWithSynchronousDelegate_InstanceMethod()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            var f = new AsyncFor();
            await f.RunForEach(sampleData, data =>
            {
                data.Success = true;
            });

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task RunsForEveryItemInCollectionWithAsynchronousDelegate_StaticMethod()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            await sampleData.RunAsyncForEach(async data =>
            {
                data.Success = true;
                await Task.CompletedTask;
            });

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task RunsForEveryItemInCollectionWithAsynchronousDelegate_InstanceMethod()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            var f = new AsyncFor();
            await f.RunForEach(sampleData, async data =>
            {
                data.Success = true;
                await Task.CompletedTask;
            });

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task ThrowsOnSimultaneousInstanceUsage()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            var f = new AsyncFor();
            var firstTask = f.RunForEach(sampleData, async data =>
            {
                data.Success = true;
                await Task.Delay(100);
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await f.RunForEach(sampleData, async data =>
                {
                    data.Success = true;
                    await Task.Delay(100);
                });
            });
        }

        [Fact]
        public async Task CapturesAllExceptions()
        {
            var sampleData = new List<TestClass>
            {
                new TestClass(),
                new TestClass() { ShouldThrow = true },
                new TestClass(),
                new TestClass() { ShouldThrow = true },
                new TestClass() { ShouldThrow = true }
            };

            var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await sampleData.RunAsyncForEach(async data =>
                {
                    if (data.ShouldThrow)
                    {
                        throw new AggregateException(new TestException(), new TestException());
                    }

                    data.Success = true;
                    await Task.CompletedTask;
                });
            });
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e => {
                Assert.IsType<AggregateException>(e);
                Assert.All((e as AggregateException).InnerExceptions, inner => Assert.IsType<TestException>(inner));
            });
        }

        [Fact]
        public async Task UnwrapsSingleExceptionAggregateExceptions()
        {
            var sampleData = new List<TestClass>
            {
                new TestClass(),
                new TestClass() { ShouldThrow = true },
                new TestClass(),
                new TestClass() { ShouldThrow = true },
                new TestClass() { ShouldThrow = true }
            };

            var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await sampleData.RunAsyncForEach(async data =>
                {
                    if (data.ShouldThrow)
                    {
                        throw new TestException();
                    }

                    data.Success = true;
                    await Task.CompletedTask;
                });
            });
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e => Assert.IsType<TestException>(e));
        }

        [Fact]
        public async Task RunsConcurrently()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            var taskAlreadyRunning = false;
            await sampleData.RunAsyncForEach(async data =>
            {
                if (taskAlreadyRunning)
                {
                    data.Success = true;
                }

                taskAlreadyRunning = true;
                // Delay to increase chances of concurrency
                await Task.Delay(100);
                taskAlreadyRunning = false;
            }, runSynchronously: false);

            Assert.Contains(sampleData, d => d.Success);
        }

        [Fact]
        public async Task NoConcurrencyWithSynchronousOption()
        {
            var sampleData = new List<TestClass>
            {
                new TestClass(),
                new TestClass(),
                new TestClass(),
                new TestClass(),
                new TestClass()
            };

            var taskAlreadyRunning = false;
            await sampleData.RunAsyncForEach(async data =>
            {
                if (taskAlreadyRunning)
                {
                    throw new Exception("Task is already running, so the 'RunSynchronously' option was ignored.");
                }
                taskAlreadyRunning = true;                
                data.Success = true;
                // Delay to increase chances of concurrency problems
                await Task.Delay(100);
                taskAlreadyRunning = false;
            }, runSynchronously: true);

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task BatchSizeLimitsConcurrency()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 20);

            var runningTasks = 0;
            var batchSize = 5;

            await sampleData.RunAsyncForEach(async data =>
            {
                if (runningTasks > batchSize)
                {
                    throw new Exception("Maximum task count exceeded.");
                }

                Interlocked.Increment(ref runningTasks);
                data.Success = true;
                // Delay to increase chances of concurrency problems
                await Task.Delay(100);
                Interlocked.Decrement(ref runningTasks);
            }, batchSize: batchSize);

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task ReportsProgressThroughToken()
        {
            var progressToken = new ProgressReportToken();
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            await sampleData.RunAsyncForEach(data =>
            {
                Assert.False(progressToken.IsCompleted);
                Assert.False(progressToken.IsIndeterminate);
                Assert.True(progressToken.Progress < 1);

                data.Success = true;
            }, progressReportToken: progressToken);

            Assert.True(progressToken.IsCompleted);
            Assert.False(progressToken.IsIndeterminate);
            Assert.Equal(1, progressToken.Progress, precision: 1);

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task ReportsProgressThroughInstanceProperties()
        {
            var sampleData = Enumerable.Repeat(new TestClass(), 5);

            var f = new AsyncFor();
            await f.RunForEach(sampleData, data =>
            {
                Assert.False(f.IsCompleted);
                Assert.False(f.IsIndeterminate);
                Assert.True(f.Progress < 1);

                data.Success = true;
            });

            Assert.True(f.IsCompleted);
            Assert.False(f.IsIndeterminate);
            Assert.Equal(1, f.Progress, precision: 1);

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        [Fact]
        public async Task ReportsProgressThroughInstanceEvent()
        {
            var myCustomMessage = $"Loading... ({Guid.NewGuid()})";
            var progressEventArgs = new ConcurrentBag<ProgressReportedEventArgs>();
            var completedCount = 0;
            void OnProgressChanged(object sender, ProgressReportedEventArgs e)
            {
                progressEventArgs.Add(e);
                Assert.Equal(myCustomMessage, e.Message);
                Assert.False(e.IsIndeterminate);
                Assert.True(e.Progress <= 1);
            }
            void OnCompleted(object sender, EventArgs e)
            {
                Interlocked.Increment(ref completedCount);
            }

            var sampleData = Enumerable.Repeat(new TestClass(), 5).ToList();

            var f = new AsyncFor();
            f.Message = myCustomMessage;
            f.ProgressChanged += OnProgressChanged;
            f.Completed += OnCompleted;
            f.BatchSize = 2;
            await f.RunForEach(sampleData, data =>
            {
                data.Success = true;
            });
            f.ProgressChanged -= OnProgressChanged;
            f.Completed -= OnCompleted;


            Assert.Equal(sampleData.Count, progressEventArgs.Count);            
            Assert.Equal(1, completedCount);

            var distinctProgressPercentages = progressEventArgs.Select(e => e.Progress).Distinct().ToList();
            Assert.InRange(distinctProgressPercentages.Count, 2, 3);
            Assert.All(distinctProgressPercentages, e => Assert.InRange(e, 0, 1));

            Assert.All(sampleData, data => Assert.True(data.Success));
        }

        private class TestClass
        {
            public bool Success { get; set; }
            public bool ShouldThrow { get; set; }
        }

        private class TestException : Exception
        {
        }
    }
}
