using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Streams.StreamReaders;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class StreamReaderFactory
{
    public static IStreamEventReader Create(ProviderType provider, LLMConnectClientOptions options)
    {
        var logger = options.LoggerFactory?.CreateLogger("StreamChunkParserFactory");

        switch (provider)
        {
            case ProviderType.OpenAI: return new NdjsonStreamEventReader(options);
            case ProviderType.Anthropic: return new SseStreamEventReader(options);
            case ProviderType.Google: return new SseStreamEventReader(options);
            case ProviderType.Ollama: return new NdjsonStreamEventReader(options);
            default:
                {
                    var message = $"Provider '{provider.ToString()}' is not supported.";

                    logger?.LogError(message);

                    throw new NotSupportedException(message);
                }
        }
    }
}