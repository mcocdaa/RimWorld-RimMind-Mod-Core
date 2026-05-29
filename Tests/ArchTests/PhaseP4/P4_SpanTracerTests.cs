using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_SpanTracerTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void ISpanTracer_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "ISpanTracer.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void ISpan_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "ISpan.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void InMemorySpanTracer_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "InMemorySpanTracer.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void ISpanTracer_HasBeginSpan() =>
            Assert.Contains("BeginSpan", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "ISpanTracer.cs", SearchOption.AllDirectories).First()));

        [Fact]
        public void ISpan_HasSetAttribute() =>
            Assert.Contains("SetAttribute", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "ISpan.cs", SearchOption.AllDirectories).First()));

        [Fact]
        public void ISpan_ImplementsIDisposable() =>
            Assert.Contains("IDisposable", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "ISpan.cs", SearchOption.AllDirectories).First()));
    }
}
