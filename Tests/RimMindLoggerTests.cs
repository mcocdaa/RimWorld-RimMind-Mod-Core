using System.Collections.Concurrent;
using System.Threading;
using RimMind.Core;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class RimMindLoggerTests
    {
        [Fact]
        public void Message_FromBackgroundThread_EnqueuesToBackgroundQueue()
        {
            string? loggedMessage = null;
            var originalMessage = Log.Message;
            Log.Message = msg => loggedMessage = msg;

            try
            {
                var thread = new Thread(() =>
                {
                    RimMindLogger.Message("bg message");
                });
                thread.Start();
                thread.Join();

                RimMindLogger.FlushBackgroundLogs();

                Assert.NotNull(loggedMessage);
                Assert.Contains("[RimMind-Core] bg message", loggedMessage);
            }
            finally
            {
                Log.Message = originalMessage;
            }
        }

        [Fact]
        public void Warning_FromBackgroundThread_EnqueuesWarnLevel()
        {
            string? loggedWarning = null;
            var originalWarning = Log.Warning;
            Log.Warning = msg => loggedWarning = msg;

            try
            {
                var thread = new Thread(() =>
                {
                    RimMindLogger.Warning("bg warning");
                });
                thread.Start();
                thread.Join();

                RimMindLogger.FlushBackgroundLogs();

                Assert.NotNull(loggedWarning);
                Assert.Contains("[RimMind-Core] bg warning", loggedWarning);
            }
            finally
            {
                Log.Warning = originalWarning;
            }
        }

        [Fact]
        public void Error_FromBackgroundThread_EnqueuesErrorLevel()
        {
            string? loggedError = null;
            var originalError = Log.Error;
            Log.Error = msg => loggedError = msg;

            try
            {
                var thread = new Thread(() =>
                {
                    RimMindLogger.Error("bg error");
                });
                thread.Start();
                thread.Join();

                RimMindLogger.FlushBackgroundLogs();

                Assert.NotNull(loggedError);
                Assert.Contains("[RimMind-Core] bg error", loggedError);
            }
            finally
            {
                Log.Error = originalError;
            }
        }

        [Fact]
        public void FlushBackgroundLogs_OnMainThreadAfterBackgroundEnqueue_FlushesAll()
        {
            var messages = new ConcurrentQueue<string>();
            var originalMessage = Log.Message;
            Log.Message = msg => messages.Enqueue(msg);

            try
            {
                var thread = new Thread(() =>
                {
                    RimMindLogger.Message("flush test");
                });
                thread.Start();
                thread.Join();

                RimMindLogger.FlushBackgroundLogs();

                Assert.Single(messages);
            }
            finally
            {
                Log.Message = originalMessage;
            }
        }

        [Fact]
        public void FlushBackgroundLogs_WithNoPending_DoesNothing()
        {
            RimMindLogger.FlushBackgroundLogs();
        }

        [Fact]
        public void Message_ContainsPrefix()
        {
            string? loggedMessage = null;
            var originalMessage = Log.Message;
            Log.Message = msg => loggedMessage = msg;

            try
            {
                var thread = new Thread(() =>
                {
                    RimMindLogger.Message("prefix check");
                });
                thread.Start();
                thread.Join();

                RimMindLogger.FlushBackgroundLogs();

                Assert.NotNull(loggedMessage);
                Assert.StartsWith("[RimMind-Core]", loggedMessage);
            }
            finally
            {
                Log.Message = originalMessage;
            }
        }

        [Fact]
        public void MultipleBackgroundMessages_AllFlushed()
        {
            var messages = new ConcurrentQueue<string>();
            var originalMessage = Log.Message;
            Log.Message = msg => messages.Enqueue(msg);

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var idx = i;
                    var thread = new Thread(() =>
                    {
                        RimMindLogger.Message($"msg_{idx}");
                    });
                    thread.Start();
                    thread.Join();
                }

                RimMindLogger.FlushBackgroundLogs();

                Assert.Equal(5, messages.Count);
            }
            finally
            {
                Log.Message = originalMessage;
            }
        }
    }
}
