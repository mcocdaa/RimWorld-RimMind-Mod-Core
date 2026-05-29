using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_ConversationCompactorTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void IConversationCompactor_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "IConversationCompactor.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void SummaryConversationCompactor_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "SummaryConversationCompactor.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void IConversationCompactor_HasCompactMethod()
        {
            var content = File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "IConversationCompactor.cs", SearchOption.AllDirectories).First());
            Assert.Contains("Compact", content);
        }
    }
}
