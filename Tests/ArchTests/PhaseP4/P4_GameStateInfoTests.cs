using System;
using System.IO;
using System.Linq;
using Xunit;
using Gsi = RimMind.Domain.Llm.GameStateInfo;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_GameStateInfoTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void GameStateInfo_Class_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "GameStateInfo.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Domain") && f.Contains("Llm")));

        [Fact]
        public void GameStateInfo_HasAddSection() =>
            Assert.Contains("AddSection", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "GameStateInfo.cs", SearchOption.AllDirectories)
                .First(f => f.Contains("Domain") && f.Contains("Llm"))));

        [Fact]
        public void GameStateInfo_HasToXml() =>
            Assert.Contains("ToXml", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "GameStateInfo.cs", SearchOption.AllDirectories)
                .First(f => f.Contains("Domain") && f.Contains("Llm"))));

        [Fact]
        public void GameStateInfo_HasContainsSection() =>
            Assert.Contains("ContainsSection", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "GameStateInfo.cs", SearchOption.AllDirectories)
                .First(f => f.Contains("Domain") && f.Contains("Llm"))));

        [Fact]
        public void LlmRequestEnvelope_UsesGameStateInfo_Type()
        {
            var content = File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "LlmRequestEnvelope.cs", SearchOption.AllDirectories).First());
            Assert.Contains("GameStateInfo?", content);
        }

        [Fact]
        public void ThinkContextEnricher_UsesAddSection()
        {
            var content = File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "ThinkContextEnricher.cs", SearchOption.AllDirectories).First());
            Assert.Contains("AddSection", content);
        }

        [Fact]
        public void GameStateInfo_AddSection_Fluent()
        {
            var gsi = new Gsi()
                .AddSection("perceptions", "test data")
                .AddSection("inner_voice", "hello");
            Assert.True(gsi.ContainsSection("perceptions"));
            Assert.True(gsi.ContainsSection("inner_voice"));
            Assert.False(gsi.ContainsSection("nonexistent"));
        }

        [Fact]
        public void GameStateInfo_ToXml_ProducesValidXml()
        {
            var gsi = new Gsi()
                .AddSection("perceptions", "saw a rabbit");
            var xml = gsi.ToXml();
            Assert.Contains("<perceptions>", xml);
            Assert.Contains("saw a rabbit", xml);
            Assert.Contains("</perceptions>", xml);
        }

        [Fact]
        public void GameStateInfo_ImplicitConversion_ToString()
        {
            Gsi? gsi = new Gsi()
                .AddSection("test", "value");
            string? result = gsi;
            Assert.NotNull(result);
            Assert.Contains("<test>", result);
        }

        [Fact]
        public void GameStateInfo_Null_ImplicitConversion_ReturnsNull()
        {
            Gsi? gsi = null;
            string? result = gsi;
            Assert.Null(result);
        }

        [Fact]
        public void GameStateInfo_Empty_ToXml_ReturnsEmpty()
        {
            var gsi = new Gsi();
            Assert.Equal("", gsi.ToXml());
        }

        [Fact]
        public void GameStateInfo_AddSection_IgnoresEmptyContent()
        {
            var gsi = new Gsi()
                .AddSection("empty", "")
                .AddSection("valid", "data");
            Assert.False(gsi.ContainsSection("empty"));
            Assert.True(gsi.ContainsSection("valid"));
        }

        [Fact]
        public void GameStateInfo_AddSection_IgnoresNullContent()
        {
            var gsi = new Gsi()
                .AddSection("null", null!)
                .AddSection("valid", "data");
            Assert.False(gsi.ContainsSection("null"));
            Assert.True(gsi.ContainsSection("valid"));
        }
    }
}
