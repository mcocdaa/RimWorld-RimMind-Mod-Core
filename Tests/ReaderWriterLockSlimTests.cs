using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ReaderWriterLockSlimTests
    {
        [Fact]
        public void WriteLock_ExcludesConcurrentWriters()
        {
            using var rwLock = new ReaderWriterLockSlim();
            int concurrentCount = 0;
            int maxConcurrent = 0;
            var tasks = new Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    rwLock.EnterWriteLock();
                    try
                    {
                        var current = Interlocked.Increment(ref concurrentCount);
                        var prev = maxConcurrent;
                        while (current > prev)
                            prev = Interlocked.CompareExchange(ref maxConcurrent, current, prev);
                        Thread.Sleep(10);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref concurrentCount);
                        rwLock.ExitWriteLock();
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Equal(1, maxConcurrent);
        }

        [Fact]
        public void ReadLock_AllowsConcurrentReaders()
        {
            using var rwLock = new ReaderWriterLockSlim();
            int concurrentCount = 0;
            int maxConcurrent = 0;
            var tasks = new Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    rwLock.EnterReadLock();
                    try
                    {
                        var current = Interlocked.Increment(ref concurrentCount);
                        var prev = maxConcurrent;
                        while (current > prev)
                            prev = Interlocked.CompareExchange(ref maxConcurrent, current, prev);
                        Thread.Sleep(20);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref concurrentCount);
                        rwLock.ExitReadLock();
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.True(maxConcurrent > 1, $"Expected concurrent readers > 1, got {maxConcurrent}");
        }

        [Fact]
        public void WriteLock_BlocksReaders()
        {
            using var rwLock = new ReaderWriterLockSlim();
            var writeEntered = new ManualResetEventSlim(false);
            var readAttempted = new ManualResetEventSlim(false);
            bool readBlockedByWrite = false;

            var writerTask = Task.Run(() =>
            {
                rwLock.EnterWriteLock();
                try
                {
                    writeEntered.Set();
                    Thread.Sleep(100);
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            });

            writeEntered.Wait();

            var readerTask = Task.Run(() =>
            {
                readAttempted.Set();
                rwLock.EnterReadLock();
                try
                {
                    readBlockedByWrite = true;
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            });

            readAttempted.Wait();
            Thread.Sleep(20);
            Assert.False(readBlockedByWrite, "Reader should be blocked while writer holds the lock");

            Task.WaitAll(writerTask, readerTask);
            Assert.True(readBlockedByWrite, "Reader should eventually acquire the lock after writer releases");
        }

        [Fact]
        public void Dispose_CalledOnce_DoesNotThrow()
        {
            var rwLock = new ReaderWriterLockSlim();
            rwLock.Dispose();
        }

        [Fact]
        public void Dispose_WithNullConditional_DoesNotThrow()
        {
            ReaderWriterLockSlim? rwLock = new ReaderWriterLockSlim();
            rwLock?.Dispose();
        }

        [Fact]
        public void WriteLock_TryFinally_EnsuresReleaseOnException()
        {
            using var rwLock = new ReaderWriterLockSlim();
            bool exceptionThrown = false;

            try
            {
                rwLock.EnterWriteLock();
                try
                {
                    throw new InvalidOperationException("test");
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Exception should have been thrown");
            Assert.True(rwLock.TryEnterWriteLock(0), "Lock should be released after exception in try/finally");
            rwLock.ExitWriteLock();
        }

        [Fact]
        public void ReadLock_TryFinally_EnsuresReleaseOnException()
        {
            using var rwLock = new ReaderWriterLockSlim();
            bool exceptionThrown = false;

            try
            {
                rwLock.EnterReadLock();
                try
                {
                    throw new InvalidOperationException("test");
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Exception should have been thrown");
            Assert.True(rwLock.TryEnterReadLock(0), "Lock should be released after exception in try/finally");
            rwLock.ExitReadLock();
        }

        [Fact]
        public void ConcurrentWriteOperations_NoDataCorruption()
        {
            using var rwLock = new ReaderWriterLockSlim();
            var dict = new Dictionary<string, int>();
            const int count = 100;
            var exceptions = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
            var tasks = new Task[count];

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            dict[$"key_{idx}"] = idx;
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
            Assert.Equal(count, dict.Count);
        }

        [Fact]
        public void MixedReadWrite_NoDataCorruption()
        {
            using var rwLock = new ReaderWriterLockSlim();
            var dict = new Dictionary<string, string>();
            const int writers = 50;
            const int readers = 50;
            var exceptions = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

            for (int i = 0; i < writers; i++)
                dict[$"key_{i}"] = $"initial_{i}";

            var tasks = new Task[writers + readers];

            for (int i = 0; i < writers; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            dict[$"key_{idx}"] = $"updated_{idx}";
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });
            }

            for (int i = 0; i < readers; i++)
            {
                int idx = i % writers;
                tasks[writers + i] = Task.Run(() =>
                {
                    try
                    {
                        rwLock.EnterReadLock();
                        try
                        {
                            dict.TryGetValue($"key_{idx}", out _);
                        }
                        finally
                        {
                            rwLock.ExitReadLock();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
        }
    }
}
