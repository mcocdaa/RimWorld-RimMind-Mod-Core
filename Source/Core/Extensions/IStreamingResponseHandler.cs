namespace RimMind.Core.Extensions
{
    public interface IStreamingResponseHandler
    {
        void OnChunkReceived(string chunk);
        void OnComplete(string fullResponse);
        void OnError(string error);
        string HandlerId { get; }
    }
}
