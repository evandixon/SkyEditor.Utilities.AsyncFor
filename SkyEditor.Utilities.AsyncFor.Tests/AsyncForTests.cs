using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SkyEditor.Utilities.AsyncFor.Tests
{
    public class AsyncForTests
    {
        [Fact]
        public async Task RunsForEveryNumberWithSynchronousDelegate_StaticMethod()
        {
            var sum = 0;
            var lockObject = new object();
            await AsyncFor.For(0, 10, i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
            });
            Assert.Equal(55, sum);
        }

        [Fact]
        public async Task RunsForEveryNumberWithSynchronousDelegate_InstanceMethod()
        {
            var sum = 0;
            var lockObject = new object();
            var f = new AsyncFor();
            await f.RunFor(0, 10, i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
            });
            Assert.Equal(55, sum);
        }

        [Fact]
        public async Task RunsForEveryNumberWithAsynchronousDelegate_StaticMethod()
        {
            var sum = 0;
            var lockObject = new object();
            await AsyncFor.For(0, 10, async i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
                await Task.CompletedTask;
            });
            Assert.Equal(55, sum);
        }

        [Fact]
        public async Task RunsForEveryNumberWithAsynchronousDelegate_InstanceMethod()
        {
            var sum = 0;
            var lockObject = new object();
            var f = new AsyncFor();
            await f.RunFor(0, 10, async i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
                await Task.CompletedTask;
            });
            Assert.Equal(55, sum);
        }

        [Fact]
        public async Task UsesStepCount()
        {
            var sum = 0;
            var lockObject = new object();
            await AsyncFor.For(0, 10, i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
            }, stepCount: 2);
            Assert.Equal(30, sum);
        }

        [Fact]
        public async Task RunsBackwardsWithNegativeStepCount()
        {
            var sum = 0;
            var lockObject = new object();
            await AsyncFor.For(10, 0, i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
            }, stepCount: -2);
            Assert.Equal(30, sum);
        }

        [Fact]
        public async Task DoesNothingWhenEndComesBeforeStart()
        {
            var sum = 0;
            var lockObject = new object();
            await AsyncFor.For(0, -10, i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
            });
            Assert.Equal(0, sum);
        }

        [Fact]
        public async Task ThrowsOnZeroStepCount()
        {
            var sum = 0;
            var lockObject = new object();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await AsyncFor.For(0, 10, i =>
                {
                    lock (lockObject)
                    {
                        sum += i;
                    }
                }, stepCount: 0);
            });
        }

        [Fact]
        public async Task ThrowsOnSimultaneousInstanceUsage()
        {
            var sum = 0;
            var lockObject = new object();
            var f = new AsyncFor();
            var first = f.RunFor(0, 10, async i =>
            {
                lock (lockObject)
                {
                    sum += i;
                }
                await Task.Delay(100);
            });
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await f.RunFor(0, 10, async i =>
                {
                    lock (lockObject)
                    {
                        sum += i;
                    }
                    await Task.Delay(100);
                });
            });
        }

        [Fact]
        public async Task CapturesAllExceptions()
        {
            var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await AsyncFor.For(0, 6, i =>
                {
                    if (i % 2 == 1)
                    {
                        throw new AggregateException(new TestException(), new TestException());
                    }
                });
            });
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e =>
            {
                Assert.IsType<AggregateException>(e);
                Assert.All((e as AggregateException).InnerExceptions, inner => Assert.IsType<TestException>(inner));
            });
        }

        [Fact]
        public async Task UnwrapsSingleExceptionAggregateExceptions()
        {
            var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await AsyncFor.For(0, 6, i =>
                {
                    if (i % 2 == 1)
                    {
                        throw new TestException();
                    }
                });
            });
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e => Assert.IsType<TestException>(e));
        }

        [Fact]
        public async Task RunsConcurrently()
        {            
            var taskAlreadyRunning = false;
            var anyConcurrency = false;
            await AsyncFor.For(0, 6, async i =>
            {
                if (taskAlreadyRunning)
                {
                    anyConcurrency = true;
                }

                taskAlreadyRunning = true;
                // Delay to increase chances of concurrency
                await Task.Delay(100);
                taskAlreadyRunning = false;
            }, runSynchronously: false);

            Assert.True(anyConcurrency);
        }

        [Fact]
        public async Task NoConcurrencyWithSynchronousOption()
        {
            var taskAlreadyRunning = false;
            await AsyncFor.For(0, 6, async i =>
            {
                if (taskAlreadyRunning)
                {
                    throw new Exception("Task is already running, so the 'RunSynchronously' option was ignored.");
                }

                taskAlreadyRunning = true;
                // Delay to increase chances of concurrency
                await Task.Delay(100);
                taskAlreadyRunning = false;
            }, runSynchronously: true);
        }

        [Fact]
        public async Task BatchSizeLimitsConcurrency()
        {
            var runningTasks = 0;
            var batchSize = 5;

            await AsyncFor.For(0, 20, async i =>
            {
                if (runningTasks > batchSize)
                {
                    throw new Exception("Maximum task count exceeded.");
                }

                Interlocked.Increment(ref runningTasks);

                // Delay to increase chances of concurrency problems
                await Task.Delay(100);

                Interlocked.Decrement(ref runningTasks);
            }, batchSize: batchSize);
        }

        [Fact]
        public async Task ReportsProgressThroughToken()
        {
            var progressToken = new ProgressReportToken();

            await AsyncFor.For(1, 10, i =>
            {
                Assert.False(progressToken.IsCompleted);
                Assert.False(progressToken.IsIndeterminate);
                Assert.True(progressToken.Progress < 1);
            }, progressReportToken: progressToken, batchSize: 2);

            Assert.True(progressToken.IsCompleted);
            Assert.False(progressToken.IsIndeterminate);
            Assert.Equal(1, progressToken.Progress, precision: 1);
        }

        [Fact]
        public async Task ReportsProgressThroughInstanceProperties()
        {
            var f = new AsyncFor();
            await f.RunFor(0, 6, i =>
            {
                Assert.False(f.IsCompleted);
                Assert.False(f.IsIndeterminate);
                Assert.True(f.Progress < 1);
            });

            Assert.True(f.IsCompleted);
            Assert.False(f.IsIndeterminate);
            Assert.Equal(1, f.Progress, precision: 1);
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

            var f = new AsyncFor();
            f.Message = myCustomMessage;
            f.ProgressChanged += OnProgressChanged;
            f.Completed += OnCompleted;
            f.BatchSize = 2;
            await f.RunFor(1, 10, async data =>
            {
                await Task.CompletedTask;
            });
            f.ProgressChanged -= OnProgressChanged;
            f.Completed -= OnCompleted;


            Assert.Equal(10, progressEventArgs.Count);
            Assert.Equal(1, completedCount);

            var distinctProgressPercentages = progressEventArgs.Select(e => e.Progress).Distinct().ToList();
            Assert.InRange(distinctProgressPercentages.Count, 2, 10);
            Assert.All(distinctProgressPercentages, p => Assert.InRange(p, 0, 1));
            Assert.Contains(distinctProgressPercentages, p => p != 0 && p != 1);
        }

        private async Task temp()
        {
            var progressToken = new ProgressReportToken();
            progressToken.ProgressChanged += (object sender, ProgressReportedEventArgs e) =>
            {
                Console.WriteLine($"Progress: {e.Progress * 100} %");
            };
            progressToken.Completed += (object sender, EventArgs e) =>
             {
                 Console.WriteLine("Completed!");
             };

            await AsyncFor.For(0, 10, i =>
            {
                Console.WriteLine(i);
            }, progressReportToken: progressToken);
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
