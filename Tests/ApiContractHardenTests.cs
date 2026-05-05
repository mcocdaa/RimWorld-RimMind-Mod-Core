using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;
using RimMind.Core.Client;
using RimMind.Core.Context;

namespace RimMind.Core.Tests
{
    public class AIResponseInitSetterTests
    {
        [Fact]
        public void Ok_Factory_SetsInitProperties()
        {
            var response = AIResponse.Ok("req-1", "hello", 42);
            Assert.True(response.Success);
            Assert.Equal("req-1", response.RequestId);
            Assert.Equal(AIRequestState.Completed, response.State);
        }

        [Fact]
        public void Failure_Factory_SetsInitProperties()
        {
            var response = AIResponse.Failure("req-2", "error msg");
            Assert.False(response.Success);
            Assert.Equal("req-2", response.RequestId);
            Assert.Equal(AIRequestState.Error, response.State);
        }

        [Fact]
        public void Cancelled_Factory_SetsInitProperties()
        {
            var response = AIResponse.Cancelled("req-3", "timeout");
            Assert.False(response.Success);
            Assert.Equal("req-3", response.RequestId);
            Assert.Equal(AIRequestState.Cancelled, response.State);
        }

        [Fact]
        public void ObjectInitializer_SetsInitProperties()
        {
            var response = new AIResponse
            {
                Success = true,
                RequestId = "init-test",
                State = AIRequestState.Completed
            };
            Assert.True(response.Success);
            Assert.Equal("init-test", response.RequestId);
            Assert.Equal(AIRequestState.Completed, response.State);
        }

        [Fact]
        public void MutableProperties_CanBeSetAfterInit()
        {
            var response = AIResponse.Ok("req-4", "content", 10);
            response.Content = "updated";
            response.Error = "err";
            response.TokensUsed = 99;
            response.PromptTokens = 50;
            response.CompletionTokens = 49;
            response.CachedTokens = 10;
            response.Priority = AIRequestPriority.High;
            response.AttemptCount = 3;
            response.QueueWaitMs = 100;
            response.ProcessingMs = 200;
            response.HttpStatusCode = 200;
            response.RequestPayloadBytes = 1024;
            response.ToolCallsJson = "{}";
            response.ReasoningContent = "thinking";
            Assert.Equal("updated", response.Content);
            Assert.Equal(99, response.TokensUsed);
        }

        [Fact]
        public void DefaultState_IsQueued()
        {
            var response = new AIResponse();
            Assert.Equal(AIRequestState.Queued, response.State);
        }

        [Fact]
        public void DefaultSuccess_IsFalse()
        {
            var response = new AIResponse();
            Assert.False(response.Success);
        }

        [Fact]
        public void DefaultRequestId_IsEmptyString()
        {
            var response = new AIResponse();
            Assert.Equal(string.Empty, response.RequestId);
        }
    }

    public class IContextEngineInterfaceTests
    {
        [Fact]
        public void IContextEngine_RequiresBuildSnapshot()
        {
            var method = typeof(IContextEngine).GetMethod("BuildSnapshot");
            Assert.NotNull(method);
            Assert.Equal(typeof(ContextSnapshot), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_RequiresGetL0CacheCount()
        {
            var method = typeof(IContextEngine).GetMethod("GetL0CacheCount");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_RequiresGetL1BlockCacheCount()
        {
            var method = typeof(IContextEngine).GetMethod("GetL1BlockCacheCount");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_RequiresGetDiffStoreCount()
        {
            var method = typeof(IContextEngine).GetMethod("GetDiffStoreCount");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_RequiresGetEmbedCacheCount()
        {
            var method = typeof(IContextEngine).GetMethod("GetEmbedCacheCount");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_RequiresResetCaches()
        {
            var method = typeof(IContextEngine).GetMethod("ResetCaches");
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method.ReturnType);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeInvalidateLayer()
        {
            var method = typeof(IContextEngine).GetMethod("InvalidateLayer");
            Assert.Null(method);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeInvalidateKey()
        {
            var method = typeof(IContextEngine).GetMethod("InvalidateKey");
            Assert.Null(method);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeUpdateBaseline()
        {
            var method = typeof(IContextEngine).GetMethod("UpdateBaseline");
            Assert.Null(method);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeInvalidateNpc()
        {
            var method = typeof(IContextEngine).GetMethod("InvalidateNpc");
            Assert.Null(method);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeTouchCache()
        {
            var method = typeof(IContextEngine).GetMethod("TouchCache");
            Assert.Null(method);
        }

        [Fact]
        public void IContextEngine_DoesNotExposeRemoveL0CacheForNpc()
        {
            var method = typeof(IContextEngine).GetMethod("RemoveL0CacheForNpc");
            Assert.Null(method);
        }

        [Fact]
        public void ContextEngine_ImplementsIContextEngine()
        {
            Assert.True(typeof(IContextEngine).IsAssignableFrom(typeof(ContextEngine)));
        }
    }

    public class ModCooldownGettersTypeTests
    {
        [Fact]
        public void ConcurrentDictionary_IsIReadOnlyDictionary()
        {
            var dict = new ConcurrentDictionary<string, Func<int>>();
            IReadOnlyDictionary<string, Func<int>> readOnly = dict;
            Assert.NotNull(readOnly);
        }

        [Fact]
        public void IReadOnlyDictionary_DoesNotExposeTryAdd()
        {
            var iface = typeof(IReadOnlyDictionary<string, Func<int>>);
            var method = iface.GetMethod("TryAdd");
            Assert.Null(method);
        }

        [Fact]
        public void IReadOnlyDictionary_DoesNotExposeClear()
        {
            var iface = typeof(IReadOnlyDictionary<string, Func<int>>);
            var method = iface.GetMethod("Clear");
            Assert.Null(method);
        }
    }
}
