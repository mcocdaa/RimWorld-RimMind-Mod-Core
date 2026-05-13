using RimMind.Application.Features.Context;
using RimMind.Application.Common.Interfaces.Context;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class ContextRequestTests
    {
        [Fact]
        public void MaxTokens_Default_Is800()
        {
            var req = new ContextRequest();
            Assert.Equal(800, req.MaxTokens);
        }

        [Fact]
        public void MaxTokens_CanBeOverridden()
        {
            var req = new ContextRequest { MaxTokens = 1600 };
            Assert.Equal(1600, req.MaxTokens);
        }
    }
}
