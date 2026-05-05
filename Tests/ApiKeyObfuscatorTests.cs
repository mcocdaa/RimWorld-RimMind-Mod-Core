using RimMind.Core.Settings;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ApiKeyObfuscatorTests
    {
        [Fact]
        public void Obfuscate_NormalKey_HasPrefix()
        {
            var result = ApiKeyObfuscator.Obfuscate("sk-test123");

            Assert.StartsWith(ApiKeyObfuscator.ObfuscationPrefix, result);
        }

        [Fact]
        public void Obfuscate_NormalKey_NotPlaintext()
        {
            var plain = "sk-test123";
            var result = ApiKeyObfuscator.Obfuscate(plain);

            Assert.DoesNotContain(plain, result.Substring(ApiKeyObfuscator.ObfuscationPrefix.Length));
        }

        [Fact]
        public void Deobfuscate_ObfuscatedKey_RestoresOriginal()
        {
            var plain = "sk-test123";
            var obfuscated = ApiKeyObfuscator.Obfuscate(plain);
            var restored = ApiKeyObfuscator.Deobfuscate(obfuscated);

            Assert.Equal(plain, restored);
        }

        [Fact]
        public void Obfuscate_RoundTrip_UnicodeKey()
        {
            var plain = "sk-中文密钥🔑";
            var restored = ApiKeyObfuscator.Deobfuscate(ApiKeyObfuscator.Obfuscate(plain));

            Assert.Equal(plain, restored);
        }

        [Fact]
        public void Obfuscate_NullInput_ReturnsNull()
        {
            var result = ApiKeyObfuscator.Obfuscate(null!);

            Assert.Null(result);
        }

        [Fact]
        public void Obfuscate_EmptyInput_ReturnsEmpty()
        {
            var result = ApiKeyObfuscator.Obfuscate(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Deobfuscate_NullInput_ReturnsNull()
        {
            var result = ApiKeyObfuscator.Deobfuscate(null!);

            Assert.Null(result);
        }

        [Fact]
        public void Deobfuscate_EmptyInput_ReturnsEmpty()
        {
            var result = ApiKeyObfuscator.Deobfuscate(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Deobfuscate_PlaintextWithoutPrefix_ReturnsAsIs()
        {
            var plain = "sk-plaintext-no-prefix";
            var result = ApiKeyObfuscator.Deobfuscate(plain);

            Assert.Equal(plain, result);
        }

        [Fact]
        public void Deobfuscate_InvalidBase64_ReturnsAsIs()
        {
            var invalid = ApiKeyObfuscator.ObfuscationPrefix + "!!!not-base64!!!";
            var result = ApiKeyObfuscator.Deobfuscate(invalid);

            Assert.Equal(invalid, result);
        }

        [Fact]
        public void ObfuscatePrefix_IsExpectedValue()
        {
            Assert.Equal("RM_OBF:", ApiKeyObfuscator.ObfuscationPrefix);
        }

        [Fact]
        public void Obfuscate_LongKey_RoundTrips()
        {
            var plain = new string('a', 5000);
            var restored = ApiKeyObfuscator.Deobfuscate(ApiKeyObfuscator.Obfuscate(plain));

            Assert.Equal(plain, restored);
        }
    }
}
