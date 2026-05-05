using System;
using RimMind.Core.Client;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ErrorBoundaryTests
    {
        [Fact]
        public void TransientExceptionChecker_TimeoutException_IsTransient()
        {
            var ex = new TimeoutException("connection timed out");
            Assert.True(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException5xx_IsTransient()
        {
            var ex = new HttpHelper.HttpException(500, "Internal Server Error");
            Assert.True(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException502_IsTransient()
        {
            var ex = new HttpHelper.HttpException(502, "Bad Gateway");
            Assert.True(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException503_IsTransient()
        {
            var ex = new HttpHelper.HttpException(503, "Service Unavailable");
            Assert.True(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException4xx_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(400, "Bad Request");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException401_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(401, "Unauthorized");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException403_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(403, "Forbidden");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException404_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(404, "Not Found");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException429_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(429, "Too Many Requests");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_ArgumentException_IsNotTransient()
        {
            var ex = new ArgumentException("invalid argument");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_NullReferenceException_IsNotTransient()
        {
            var ex = new NullReferenceException();
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_InvalidOperationException_IsNotTransient()
        {
            var ex = new InvalidOperationException("invalid operation");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void TransientExceptionChecker_HttpException600_IsNotTransient()
        {
            var ex = new HttpHelper.HttpException(600, "Non-standard");
            Assert.False(TransientExceptionChecker.IsTransient(ex));
        }

        [Fact]
        public void AIDebugLog_Record_NullSettings_DoesNotThrow()
        {
            RimMindCoreMod.Settings = null;
            var request = new AIRequest
            {
                RequestId = "test_null_settings",
                Messages = new System.Collections.Generic.List<ChatMessage>()
            };
            var response = AIResponse.Ok("test_null_settings", "ok", 10);

            var exception = Record.Exception(() => AIDebugLog.Record(request, response, 100));
            Assert.Null(exception);
        }

        [Fact]
        public void AIDebugLog_Record_SettingsWithModelName_DoesNotThrow()
        {
            RimMindCoreMod.Settings = new AICoreSettings { modelName = "test-model" };
            var request = new AIRequest
            {
                RequestId = "test_with_model",
                Messages = new System.Collections.Generic.List<ChatMessage>()
            };
            var response = AIResponse.Ok("test_with_model", "ok", 10);

            var exception = Record.Exception(() => AIDebugLog.Record(request, response, 100));
            Assert.Null(exception);

            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void NpcChatResult_ErrorProperty_StoresMessage()
        {
            var result = new NpcChatResult { Error = "test error message" };
            Assert.Equal("test error message", result.Error);
        }

        [Fact]
        public void NpcChatResult_DefaultError_IsNull()
        {
            var result = new NpcChatResult();
            Assert.Null(result.Error);
        }
    }
}
