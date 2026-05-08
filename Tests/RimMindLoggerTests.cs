using System.Collections.Concurrent;
using System.Threading;
using RimMind.Contracts.Internal;
using RimMind.Core.Internal;
using RimMind.Kernel.Abstractions;
using RimMind.Kernel.Logging;
using Xunit;

namespace RimMind.Core.Tests
{
    internal class TestLogSink : ILogSink
    {
        public ConcurrentQueue<(string level, string message)> Messages { get; } = new();

        public void Message(string msg) => Messages.Enqueue(("Message", msg));
        public void Warning(string msg) => Messages.Enqueue(("Warning", msg));
        public void Error(string msg) => Messages.Enqueue(("Error", msg));
    }

    public class RimMindLoggerTests
    {
        private readonly TestLogSink _sink = new();

        public RimMindLoggerTests()
        {
            RimMindServiceLocator.Register<ILogSink>(_sink);
        }

        [Fact]
        public void Message_FromBackgroundThread_EnqueuesToBackgroundQueue()
        {
            var thread = new Thread(() =>
            {
                RimMindLogger.Message("bg message");
            });
            thread.Start();
            thread.Join();

            RimMindLogger.FlushBackgroundLogs();

            Assert.Contains(_sink.Messages, m => m.level == "Message" && m.message.Contains("[RimMind-Core] bg message"));
        }

        [Fact]
        public void Warning_FromBackgroundThread_EnqueuesWarnLevel()
        {
            var thread = new Thread(() =>
            {
                RimMindLogger.Warning("bg warning");
            });
            thread.Start();
            thread.Join();

            RimMindLogger.FlushBackgroundLogs();

            Assert.Contains(_sink.Messages, m => m.level == "Warning" && m.message.Contains("[RimMind-Core] bg warning"));
        }

        [Fact]
        public void Error_FromBackgroundThread_EnqueuesErrorLevel()
        {
            var thread = new Thread(() =>
            {
                RimMindLogger.Error("bg error");
            });
            thread.Start();
            thread.Join();

            RimMindLogger.FlushBackgroundLogs();

            Assert.Contains(_sink.Messages, m => m.level == "Error" && m.message.Contains("[RimMind-Core] bg error"));
        }

        [Fact]
        public void FlushBackgroundLogs_OnMainThreadAfterBackgroundEnqueue_FlushesAll()
        {
            var thread = new Thread(() =>
            {
                RimMindLogger.Message("flush test");
            });
            thread.Start();
            thread.Join();

            RimMindLogger.FlushBackgroundLogs();

            Assert.Single(_sink.Messages);
        }

        [Fact]
        public void FlushBackgroundLogs_WithNoPending_DoesNothing()
        {
            RimMindLogger.FlushBackgroundLogs();
        }

        [Fact]
        public void Message_ContainsPrefix()
        {
            var thread = new Thread(() =>
            {
                RimMindLogger.Message("prefix check");
            });
            thread.Start();
            thread.Join();

            RimMindLogger.FlushBackgroundLogs();

            Assert.Contains(_sink.Messages, m => m.message.StartsWith("[RimMind-Core]"));
        }

        [Fact]
        public void MultipleBackgroundMessages_AllFlushed()
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

            Assert.Equal(5, _sink.Messages.Count);
        }
    }
}
