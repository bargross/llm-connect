using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class StreamChunkParserFactory
{
    public static IStreamChunkParser Create(ProviderType provider, LLMConnectClientOptions options)
    {
        var logger = options.LoggerFactory?.CreateLogger("StreamChunkParserFactory");

        switch (provider)
        {
            case ProviderType.OpenAI: return new OpenAIStreamChunkParser(options);
            case ProviderType.Anthropic: return new AnthropicStreamChunkParser(options);
            case ProviderType.Google: return new GoogleStreamChunkParser(options);
            case ProviderType.Ollama: return new OllamaStreamChunkParser(options);
            default:
                {
                    var message = $"Provider '{provider.ToString()}' is not supported.";

                    logger?.LogError(message);

                    throw new NotSupportedException(message);
                }
        }
    }
}