using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    /// <summary>
    /// Task 13: 验证 RimMindAPI 已暴露 AddMiddleware&lt;TContext&gt; 作为子 mod 注册自定义 middleware 的公共入口。
    /// 该方法直接委托 RimMindRuntime.AddMiddleware(调用 MutablePipeline.Use 于已构建的 pipeline),
    /// 支持子 mod 在 Compose() 后的 Initialize 阶段晚期注册。
    /// 注:原计划拟新建 RimMindAPI.Pipeline 门面类,经核查 RimMindAPI.AddMiddleware&lt;TContext&gt; 已存在且更通用(泛型),
    /// 新建窄化门面会造成重复入口点(逻辑二路),故改为契约验证。
    /// 注2:VerseStubs.cs 中有 RimMindAPI 桩(仅含 Modes/Request),反射会解析到桩,故用源码文本检查(与 R_A3/R_B3 模式一致)。
    /// </summary>
    public class R_N10_RimMindAPIPipelineExtensibilityTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        private static readonly string RimMindApiPath = Path.Combine(SourceRoot, "RimMindAPI.cs");

        [Fact]
        [Trait("Phase", "N")]
        public void RimMindAPI_SourceFile_ShouldExist()
        {
            File.Exists(RimMindApiPath).Should().BeTrue(
                "RimMindAPI.cs must exist for source-text contract verification");
        }

        [Fact]
        [Trait("Phase", "N")]
        public void RimMindAPI_ShouldExpose_AddMiddleware_GenericMethod()
        {
            var content = File.ReadAllText(RimMindApiPath);
            content.Should().Contain("public static void AddMiddleware<TContext>",
                "RimMindAPI.AddMiddleware<TContext> is the public extensibility point for sub-mods to register middleware");
            content.Should().Contain("where TContext : IPipelineContext",
                "TContext must be constrained to IPipelineContext");
            content.Should().Contain("IMiddleware<TContext> middleware",
                "parameter must be IMiddleware<TContext>");
        }

        [Fact]
        [Trait("Phase", "N")]
        public void RimMindAPI_AddMiddleware_ShouldDelegate_To_RuntimeInstance()
        {
            var content = File.ReadAllText(RimMindApiPath);
            content.Should().Contain("RimMindRuntime.Instance.AddMiddleware",
                "AddMiddleware must delegate to RimMindRuntime.AddMiddleware (calls MutablePipeline.Use on live pipelines, not snapshot registry)");
        }
    }
}
