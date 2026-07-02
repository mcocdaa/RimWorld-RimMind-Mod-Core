using System;
using System.Linq;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    /// <summary>
    /// RimMindErrorCode 枚举测试：验证错误码分组和值范围。迁移自 RimMind-Actions/Tests。
    /// </summary>
    public class RimMindErrorCodeTests
    {
        [Fact]
        public void 客户端错误码在1000段()
        {
            Assert.Equal(1000, (int)RimMindErrorCode.ClientNotConfigured);
            Assert.Equal(1001, (int)RimMindErrorCode.ClientTransientFailure);
            Assert.Equal(1002, (int)RimMindErrorCode.ClientPermanentFailure);
            Assert.Equal(1003, (int)RimMindErrorCode.ClientCircuitOpen);
        }

        [Fact]
        public void 管线错误码在2000段()
        {
            Assert.Equal(2000, (int)RimMindErrorCode.ContextBuildFailed);
            Assert.Equal(2001, (int)RimMindErrorCode.PipelineShortCircuited);
        }

        [Fact]
        public void 工具和机制错误码在3000段()
        {
            Assert.Equal(3000, (int)RimMindErrorCode.ToolNotFound);
            Assert.Equal(3001, (int)RimMindErrorCode.ToolExecutionFailed);
            Assert.Equal(3002, (int)RimMindErrorCode.ToolPolicyDenied);
            Assert.Equal(3003, (int)RimMindErrorCode.ToolMaxDepthExceeded);
            Assert.Equal(3010, (int)RimMindErrorCode.MechanismOperationNotSupported);
            Assert.Equal(3011, (int)RimMindErrorCode.MechanismPawnNotFound);
            Assert.Equal(3012, (int)RimMindErrorCode.MechanismInvalidDefName);
            Assert.Equal(3013, (int)RimMindErrorCode.MechanismMapNotFound);
            Assert.Equal(3014, (int)RimMindErrorCode.MechanismInvalidAction);
        }

        [Fact]
        public void NPC和远程错误码在4000段()
        {
            Assert.Equal(4000, (int)RimMindErrorCode.NpcNotFound);
            Assert.Equal(4001, (int)RimMindErrorCode.RemoteBackendFailed);
        }

        [Fact]
        public void 内部错误码在9000段()
        {
            Assert.Equal(9000, (int)RimMindErrorCode.InternalError);
            Assert.Equal(9001, (int)RimMindErrorCode.NotImplemented);
            Assert.Equal(9002, (int)RimMindErrorCode.Cancelled);
            Assert.Equal(9003, (int)RimMindErrorCode.Timeout);
        }

        [Fact]
        public void 所有错误码值唯一()
        {
            var values = Enum.GetValues(typeof(RimMindErrorCode)).Cast<int>().ToList();
            var distinct = values.Distinct().ToList();
            Assert.Equal(values.Count, distinct.Count);
        }
    }
}
