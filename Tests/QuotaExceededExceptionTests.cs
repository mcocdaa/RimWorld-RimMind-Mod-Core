using RimMind.Core.Client;
using Xunit;

namespace RimMind.Core.Tests
{
    public class QuotaExceededExceptionTests
    {
        [Fact]
        public void IsQuotaError_QuotaExceededTag_ReturnsTrue()
        {
            Assert.True(QuotaExceededException.IsQuotaError("[QuotaExceeded] limit reached"));
        }

        [Fact]
        public void IsQuotaError_QuotaKeyword_ReturnsTrue()
        {
            Assert.True(QuotaExceededException.IsQuotaError("quota exceeded for this month"));
        }

        [Fact]
        public void IsQuotaError_InsufficientBalance_ReturnsTrue()
        {
            Assert.True(QuotaExceededException.IsQuotaError("insufficient_balance error"));
        }

        [Fact]
        public void IsQuotaError_PaymentRequired_ReturnsTrue()
        {
            Assert.True(QuotaExceededException.IsQuotaError("payment_required to continue"));
        }

        [Fact]
        public void IsQuotaError_CaseInsensitive_ReturnsTrue()
        {
            Assert.True(QuotaExceededException.IsQuotaError("QUOTA exceeded"));
            Assert.True(QuotaExceededException.IsQuotaError("[quotaexceeded]"));
        }

        [Fact]
        public void IsQuotaError_NormalError_ReturnsFalse()
        {
            Assert.False(QuotaExceededException.IsQuotaError("network timeout"));
        }

        [Fact]
        public void IsQuotaError_EmptyString_ReturnsFalse()
        {
            Assert.False(QuotaExceededException.IsQuotaError(""));
        }

        [Fact]
        public void IsQuotaError_NullString_ReturnsFalse()
        {
            Assert.False(QuotaExceededException.IsQuotaError(null!));
        }

        [Fact]
        public void DefaultConstructor_SetsMessage()
        {
            var ex = new QuotaExceededException();
            Assert.Contains("quota", ex.Message.ToLower());
        }

        [Fact]
        public void CustomMessage_SetsMessage()
        {
            var ex = new QuotaExceededException("custom message");
            Assert.Equal("custom message", ex.Message);
        }
    }
}
