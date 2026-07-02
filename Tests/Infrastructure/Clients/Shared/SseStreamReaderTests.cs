using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Infrastructure.Services.Clients.Shared;
using Xunit;

namespace RimMind.Tests.Infrastructure.Clients.Shared
{
    public class SseStreamReaderTests
    {
        [Fact]
        public async Task ReadDataLinesAsync_SingleDataLine_InvokesCallback()
        {
            var lines = new[] { "data: {\"hello\":\"world\"}" };
            var received = new List<string>();
            using var reader = CreateReader(lines);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Single(received);
            Assert.Equal("{\"hello\":\"world\"}", received[0]);
        }

        [Fact]
        public async Task ReadDataLinesAsync_MultipleDataLines_InvokesCallbackForEach()
        {
            var lines = new[]
            {
                "data: chunk1",
                "data: chunk2",
                "data: chunk3",
            };
            var received = new List<string>();
            using var reader = CreateReader(lines);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(3, received.Count);
            Assert.Equal("chunk1", received[0]);
            Assert.Equal("chunk2", received[1]);
            Assert.Equal("chunk3", received[2]);
        }

        [Fact]
        public async Task ReadDataLinesAsync_DoneSentinel_StopsReading()
        {
            var lines = new[]
            {
                "data: before",
                "data: [DONE]",
                "data: after",
            };
            var received = new List<string>();
            using var reader = CreateReader(lines);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Single(received);
            Assert.Equal("before", received[0]);
        }

        [Fact]
        public async Task ReadDataLinesAsync_SkipsEmptyLines()
        {
            var lines = new[]
            {
                "",
                "data: first",
                "",
                "data: second",
                "",
            };
            var received = new List<string>();
            using var reader = CreateReader(lines);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(2, received.Count);
        }

        [Fact]
        public async Task ReadDataLinesAsync_SkipsNonDataLines()
        {
            var lines = new[]
            {
                ": comment",
                "event: message",
                "id: 123",
                "data: payload",
            };
            var received = new List<string>();
            using var reader = CreateReader(lines);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Single(received);
            Assert.Equal("payload", received[0]);
        }

        [Fact]
        public async Task ReadDataLinesAsync_CancellationRequested_StopsReading()
        {
            var lines = new[]
            {
                "data: chunk1",
                "data: chunk2",
                "data: chunk3",
            };
            var received = new List<string>();
            using var reader = CreateReader(lines);
            using var cts = new CancellationTokenSource();

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                cts.Cancel();
                return Task.CompletedTask;
            }, cts.Token);

            Assert.Single(received);
        }

        [Fact]
        public async Task ReadDataLinesAsync_EmptyStream_CompletesWithoutCallback()
        {
            var received = new List<string>();
            using var reader = CreateReader(new string[0]);

            await SseStreamReader.ReadDataLinesAsync(reader, data =>
            {
                received.Add(data);
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Empty(received);
        }

        private static StreamReader CreateReader(IEnumerable<string> lines)
        {
            var text = string.Join("\n", lines);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return new StreamReader(stream);
        }
    }
}
