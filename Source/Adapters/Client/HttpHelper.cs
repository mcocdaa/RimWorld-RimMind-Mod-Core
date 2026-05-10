using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RimMind.Adapters.Client
{
    internal static class HttpHelper
    {
        private static readonly HttpClient _http = new HttpClient();

        public class HttpException : Exception
        {
            public int StatusCode { get; }
            public HttpException(string message, int statusCode) : base(message) { StatusCode = statusCode; }
        }

        public static async Task<(string body, long statusCode)> PostAsync(
            string url, string jsonBody, string? authHeader = null,
            string? headerName = null, string? headerValue = null,
            float connectTimeout = 60f)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (authHeader != null)
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            if (headerName != null && headerValue != null)
                request.Headers.TryAddWithoutValidation(headerName, headerValue);

            using var response = await _http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return (body, (long)response.StatusCode);
        }

        public static async Task<(string body, long statusCode)> GetAsync(
            string url, string? authHeader = null,
            string? headerName = null, string? headerValue = null,
            float connectTimeout = 60f)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (authHeader != null)
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            if (headerName != null && headerValue != null)
                request.Headers.TryAddWithoutValidation(headerName, headerValue);

            using var response = await _http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return (body, (long)response.StatusCode);
        }
    }
}
