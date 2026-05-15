using System;
using RimMind.Infrastructure.Services.Clients;

namespace RimMind.IntegrationTests.Infrastructure.Services
{
    public class HttpHelperTests
    {
        [Fact]
        public void HttpException_ShouldStoreStatusCode()
        {
            var ex = new HttpHelper.HttpException("test error", 429);
            ex.StatusCode.Should().Be(429);
            ex.Message.Should().Be("test error");
        }
    }
}
