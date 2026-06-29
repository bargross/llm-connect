using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal static class StreamChunkParserFactory
{
    public static IStreamChunkParser Create(ProviderType provider, LLMConnectClientOptions options)
    {
        return provider switch
        {
            ProviderType.OpenAI => new OpenAIStreamChunkParser(options),
            ProviderType.Anthropic => new AnthropicStreamChunkParser(options),
            ProviderType.Google => new GoogleStreamChunkParser(options),
            ProviderType.Ollama => new OllamaStreamChunkParser(options),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
    }
}