using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RimMind.Core.Client;
using RimMind.Core.Internal;
using RimMind.Core.Settings;
using Newtonsoft.Json;

namespace RimMind.Core.Client.OpenAI
{
    public class OpenAIClient : IAIClient
    {
        private sealed class AIHttpException : Exception
        {
            public long StatusCode { get; }

            public AIHttpException(long statusCode, string message) : base(message)
            {
                StatusCode = statusCode;
            }
        }

        private readonly RimMindCoreSettings _settings;

        public OpenAIClient(RimMindCoreSettings settings)
        {
            _settings = settings;
        }

        public bool IsConfigured() => _settings.IsConfigured();

        public bool IsLocalEndpoint => IsLoopbackEndpoint(_settings.apiEndpoint);

        private static bool IsLoopbackEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return false;
            if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri)) return false;
            if (uri.IsLoopback) return true;
            string host = uri.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public async Task<AIResponse> SendAsync(AIRequest request)
        {
            string endpoint = FormatEndpoint(_settings.apiEndpoint);
            string json = BuildRequestJson(request);

            if (_settings.debugLogging)
                AIRequestQueue.LogFromBackground($"[RimMind] → {request.RequestId}\n{json}");

            var sw = Stopwatch.StartNew();
            try
            {
                (string responseText, long httpStatusCode) = await PostAsync(endpoint, json);
                var parsed = JsonConvert.DeserializeObject<OpenAIResponseDto>(responseText);
                string content = parsed?.choices?[0]?.message?.content ?? string.Empty;
                int tokens = parsed?.usage?.total_tokens ?? 0;
                sw.Stop();

                if (_settings.debugLogging)
                    AIRequestQueue.LogFromBackground($"[RimMind] ← {request.RequestId} ({tokens} tok)\n{content}");

                var response = AIResponse.Ok(request.RequestId, content, tokens);
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.HttpStatusCode = httpStatusCode;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;
                AIDebugLog.Record(request, response, (int)sw.ElapsedMilliseconds);
                return response;
            }
            catch (AIHttpException ex)
            {
                sw.Stop();
                AIRequestQueue.LogFromBackground($"[RimMind] Request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                var response = AIResponse.Failure(request.RequestId, ex.Message);
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.HttpStatusCode = ex.StatusCode;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;
                AIDebugLog.Record(request, response, (int)sw.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                AIRequestQueue.LogFromBackground($"[RimMind] Request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                var response = AIResponse.Failure(request.RequestId, ex.Message);
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;
                AIDebugLog.Record(request, response, (int)sw.ElapsedMilliseconds);
                return response;
            }
        }

        private string BuildRequestJson(AIRequest request)
        {
            List<MessageDto> messages;

            if (request.Messages != null && request.Messages.Count > 0)
            {
                messages = request.Messages
                    .Select(m => new MessageDto { role = m.Role, content = m.Content })
                    .ToList();
            }
            else
            {
                messages = new List<MessageDto>();
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                    messages.Add(new MessageDto { role = "system", content = request.SystemPrompt });
                messages.Add(new MessageDto { role = "user", content = request.UserPrompt });
            }

            var body = new OpenAIRequestDto
            {
                model = _settings.modelName,
                messages = messages,
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : _settings.maxTokens,
                temperature = request.Temperature,
                stream = false,
            };

            if (_settings.forceJsonMode && request.UseJsonMode)
                body.response_format = new ResponseFormatDto { type = "json_object" };

            return JsonConvert.SerializeObject(body, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private async Task<(string text, long statusCode)> PostAsync(string url, string jsonBody)
        {
            bool isLocal = IsLoopbackEndpoint(url);
            int timeoutSeconds = isLocal ? 300 : 60;
            byte[] payload = Encoding.UTF8.GetBytes(jsonBody);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = $"Bearer {_settings.apiKey}";
            request.ContentLength = payload.Length;
            request.Timeout = timeoutSeconds * 1000;
            request.ReadWriteTimeout = timeoutSeconds * 1000;

            try
            {
                using (var requestStream = await WithTimeout(request.GetRequestStreamAsync(), request, timeoutSeconds))
                    await requestStream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);

                using (var response = (HttpWebResponse)await WithTimeout(request.GetResponseAsync(), request, timeoutSeconds))
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string body = await WithTimeout(reader.ReadToEndAsync(), request, timeoutSeconds);
                    return (body, (long)response.StatusCode);
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse errorResponse)
            {
                using (errorResponse)
                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                {
                    string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    long statusCode = (long)errorResponse.StatusCode;
                    throw new AIHttpException(statusCode, $"HTTP {statusCode}: {body}");
                }
            }
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, HttpWebRequest request, int timeoutSeconds)
        {
            if (await Task.WhenAny(task, Task.Delay(timeoutSeconds * 1000)).ConfigureAwait(false) != task)
            {
                request.Abort();
                throw new TimeoutException($"Connection timeout after {timeoutSeconds}s");
            }

            return await task.ConfigureAwait(false);
        }

        private static string FormatEndpoint(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl)) return string.Empty;
            string trimmed = baseUrl.Trim().TrimEnd('/');
            if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                return trimmed;
            var uri = new Uri(trimmed);
            string path = uri.AbsolutePath.Trim('/');
            if (!string.IsNullOrEmpty(path))
                return trimmed + "/chat/completions";
            return trimmed + "/v1/chat/completions";
        }
    }
}
