using System;
using System.Text;

namespace RimMind.Contracts.Settings
{
    public static class ApiKeyObfuscator
    {
        private const byte XorKey = 0x5A;

        public static string Obfuscate(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ XorKey);
            return Convert.ToBase64String(bytes);
        }

        public static string Deobfuscate(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(key);
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte)(bytes[i] ^ XorKey);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
