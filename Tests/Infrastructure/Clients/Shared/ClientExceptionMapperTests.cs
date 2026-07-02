using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Domain.Llm;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Infrastructure.Services.Clients.Shared;
using Xunit;

namespace RimMind.Tests.Infrastructure.Clients.Shared
{
    public class ClientExceptionMapperTests
    {
        [Fact]
        public void MapException_TaskCanceledException_ReturnsCancelledError()
        {
            var ex = new TaskCanceledException();
            var logSink = new CaptureLogSink();

            var result = ClientExceptionMapper.MapException(ex, "TestClient", "req-001", "request", logSink);

            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.Cancelled, result.Error.Code);
        }

        [Fact]
        public void MapException_TaskCanceledException_LogsWarning()
        {
            var ex = new TaskCanceledException();
            var logSink = new CaptureLogSink();

            ClientExceptionMapper.MapException(ex, "TestClient", "req-001", "stream", logSink);

            Assert.Single(logSink.BackgroundMessages);
            Assert.Contains("cancelled", logSink.BackgroundMessages[0]);
            Assert.Contains("req-001", logSink.BackgroundMessages[0]);
        }

        [Fact]
        public void MapException_HttpException_ReturnsClientTransientError()
        {
            var ex = new HttpTransport.HttpException("Server error", 500);
            var logSink = new CaptureLogSink();

            var result = ClientExceptionMapper.MapException(ex, "TestClient", "req-002", "request", logSink);

            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.ClientTransientFailure, result.Error.Code);
            Assert.Contains("Server error", result.Error.Message);
        }

        [Fact]
        public void MapException_HttpException_LogsWarning()
        {
            var ex = new HttpTransport.HttpException("Bad gateway", 502);
            var logSink = new CaptureLogSink();

            ClientExceptionMapper.MapException(ex, "TestClient", "req-003", "stream", logSink);

            Assert.Single(logSink.BackgroundMessages);
            Assert.Contains("Bad gateway", logSink.BackgroundMessages[0]);
            Assert.Contains("req-003", logSink.BackgroundMessages[0]);
        }

        [Fact]
        public void MapException_GenericException_ReturnsInternalErrorByDefault()
        {
            var ex = new InvalidOperationException("Something broke");
            var logSink = new CaptureLogSink();

            var result = ClientExceptionMapper.MapException(ex, "OpenAI", "req-004", "request", logSink);

            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.InternalError, result.Error.Code);
            Assert.Contains("OpenAI", result.Error.Message);
            Assert.Contains("request", result.Error.Message);
            Assert.Contains("Something broke", result.Error.Message);
        }

        [Fact]
        public void MapException_GenericException_UseClientTransientReturnsClientTransient()
        {
            var ex = new InvalidOperationException("Something broke");
            var logSink = new CaptureLogSink();

            var result = ClientExceptionMapper.MapException(ex, "Player2", "req-005", "stream", logSink,
                useClientTransientForGeneric: true);

            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.ClientTransientFailure, result.Error.Code);
        }

        [Fact]
        public void MapException_GenericException_LogsWarning()
        {
            var ex = new InvalidOperationException("Something broke");
            var logSink = new CaptureLogSink();

            ClientExceptionMapper.MapException(ex, "TestClient", "req-006", "request", logSink);

            Assert.Single(logSink.BackgroundMessages);
            Assert.Contains("Something broke", logSink.BackgroundMessages[0]);
            Assert.Contains("req-006", logSink.BackgroundMessages[0]);
        }

        [Fact]
        public void MapException_NullLogSink_DoesNotThrow()
        {
            var ex = new InvalidOperationException("Something broke");

            var result = ClientExceptionMapper.MapException(ex, "TestClient", "req-007", "request", null);

            Assert.True(result.IsErr);
        }

        [Fact]
        public void MapException_LogMessageIncludesClientNameAndOperation()
        {
            var ex = new InvalidOperationException("fail");
            var logSink = new CaptureLogSink();

            ClientExceptionMapper.MapException(ex, "MyClient", "req-008", "stream", logSink);

            Assert.Single(logSink.BackgroundMessages);
            Assert.Contains("MyClient", logSink.BackgroundMessages[0]);
            Assert.Contains("stream", logSink.BackgroundMessages[0]);
        }

        private class CaptureLogSink : ILogSink
        {
            public List<string> BackgroundMessages { get; } = new();

            public void Message(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
            public void LogFromBackground(string msg, bool isWarning = false)
            {
                BackgroundMessages.Add(msg);
            }
        }
    }
}
