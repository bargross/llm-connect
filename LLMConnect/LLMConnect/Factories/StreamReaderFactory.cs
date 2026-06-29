using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Streams.StreamReaders;

namespace LLMConnect;

internal static class StreamReaderFactory
{
    public static IStreamEventReader Create(ProviderType provider, LLMConnectClientOptions options)
    {
        return provider switch
        {
            ProviderType.OpenAI => new NdjsonStreamEventReader(options),
            ProviderType.Anthropic => new SseStreamEventReader(options),
            ProviderType.Google => new SseStreamEventReader(options),
            ProviderType.Ollama => new NdjsonStreamEventReader(options),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
    }
}