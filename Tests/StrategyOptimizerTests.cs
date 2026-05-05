using System.Collections.Generic;
using RimMind.Core.Agent;
using RimMind.Core.Client;
using Xunit;

namespace RimMind.Core.Tests
{
    public class StrategyOptimizerTests
    {
        [Fact]
        public void AdjustWeight_NewAction_DefaultIs1()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("attack", 0.5f);

            var top = opt.GetTopN(10);
            Assert.Single(top);
            Assert.Equal("attack", top[0].Key);
            Assert.Equal(1.5f, top[0].Value, 3);
        }

        [Fact]
        public void AdjustWeight_ExistingAction_AddsDelta()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("attack", 1.0f);
            opt.AdjustWeight("attack", 0.5f);

            var top = opt.GetTopN(10);
            Assert.Single(top);
            Assert.Equal(2.5f, top[0].Value, 3);
        }

        [Fact]
        public void AdjustWeight_ClampedToUpper5()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("attack", 10f);

            var top = opt.GetTopN(10);
            Assert.Equal(5f, top[0].Value, 3);
        }

        [Fact]
        public void AdjustWeight_ClampedToLower0()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("attack", -10f);

            var top = opt.GetTopN(10);
            Assert.Equal(0f, top[0].Value, 3);
        }

        [Fact]
        public void AdjustWeight_NullOrEmpty_NoOp()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight(null!, 1f);
            opt.AdjustWeight("", 1f);

            var top = opt.GetTopN(10);
            Assert.Empty(top);
        }

        [Fact]
        public void DecayAll_MultipliesBy0999_FloorAt05()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("a", 2f);
            opt.AdjustWeight("b", -0.6f);

            opt.DecayAll();

            var top = opt.GetTopN(10);
            var dict = new Dictionary<string, float>();
            foreach (var kv in top) dict[kv.Key] = kv.Value;

            Assert.Equal(3f * 0.999f, dict["a"], 4);
            Assert.Equal(0.5f, dict["b"], 4);
        }

        [Fact]
        public void DecayAll_Below05_FlooredTo05()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("a", -0.5f);
            opt.DecayAll();

            var top = opt.GetTopN(10);
            Assert.Equal(0.5f, top[0].Value, 3);
        }

        [Fact]
        public void GetTopN_ReturnsSortedDescending()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("low", -0.5f);
            opt.AdjustWeight("mid", 0f);
            opt.AdjustWeight("high", 2f);

            var top = opt.GetTopN(3);
            Assert.Equal(3, top.Count);
            Assert.Equal("high", top[0].Key);
            Assert.Equal("mid", top[1].Key);
            Assert.Equal("low", top[2].Key);
        }

        [Fact]
        public void GetTopN_LessThanN_ReturnsAll()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("a", 1f);
            opt.AdjustWeight("b", 2f);

            var top = opt.GetTopN(10);
            Assert.Equal(2, top.Count);
        }

        [Fact]
        public void GetTopN_TruncatesToN()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("a", 1f);
            opt.AdjustWeight("b", 2f);
            opt.AdjustWeight("c", 3f);

            var top = opt.GetTopN(2);
            Assert.Equal(2, top.Count);
            Assert.Equal("c", top[0].Key);
            Assert.Equal("b", top[1].Key);
        }

        [Fact]
        public void GetWeightedTools_SortsByWeight()
        {
            var opt = new StrategyOptimizer();
            opt.AdjustWeight("tool_b", 2f);
            opt.AdjustWeight("tool_a", -0.5f);

            var tools = new List<StructuredTool>
            {
                new StructuredTool { Name = "tool_a" },
                new StructuredTool { Name = "tool_b" },
                new StructuredTool { Name = "tool_c" },
            };

            var sorted = opt.GetWeightedTools(tools);
            Assert.Equal("tool_b", sorted[0].Name);
            Assert.Equal("tool_c", sorted[1].Name);
            Assert.Equal("tool_a", sorted[2].Name);
        }

        [Fact]
        public void GetWeightedTools_Null_ReturnsNull()
        {
            var opt = new StrategyOptimizer();
            var result = opt.GetWeightedTools(null!);
            Assert.Null(result);
        }

        [Fact]
        public void GetWeightedTools_SingleElement_ReturnsSame()
        {
            var opt = new StrategyOptimizer();
            var tools = new List<StructuredTool> { new StructuredTool { Name = "only" } };
            var result = opt.GetWeightedTools(tools);
            Assert.Single(result);
            Assert.Equal("only", result[0].Name);
        }

        [Fact]
        public void GetWeightedTools_UnknownTool_Default1()
        {
            var opt = new StrategyOptimizer();
            var tools = new List<StructuredTool>
            {
                new StructuredTool { Name = "unknown_a" },
                new StructuredTool { Name = "unknown_b" },
            };

            var sorted = opt.GetWeightedTools(tools);
            Assert.Equal(2, sorted.Count);
        }
    }
}
