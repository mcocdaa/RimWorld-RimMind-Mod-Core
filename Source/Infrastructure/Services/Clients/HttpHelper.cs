using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using Verse;

namespace RimMind.Infrastructure.Services.Clients
{
    internal static class HttpHelper
    {
        internal static async Task<Result<string>> SendJsonAsync(
            string url,
            string jsonBody,
            string apiKey,
            int timeoutMs,
            string? contentType = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = contentType ?? "application/json";
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Timeout = timeoutMs;

                var bytes = Encoding.UTF8.GetBytes(jsonBody);
                request.ContentLength = bytes.Length;

                using (var stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        string responseBody = await reader.ReadToEndAsync();
                        return Result<string>.Ok(responseBody);
                    }
                }
            }
            catch (WebException ex)
            {
                string errorBody = "";
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (var errorReader = new StreamReader(errorResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        errorBody = errorReader.ReadToEnd();
                    }
                }
                string message = !string.IsNullOrEmpty(errorBody) ? errorBody : ex.Message;
                return Result<string>.Err(RimMindError.Http(message));
            }
            catch (Exception ex)
            {
                return Result<string>.Err(RimMindError.Http(ex.Message));
            }
        }
    }
}
