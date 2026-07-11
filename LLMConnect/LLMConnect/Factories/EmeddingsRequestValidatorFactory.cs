using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class EmbeddingRequestValidatorFactory
{
    public static IEmbeddingRequestValidator Create(ProviderType? provider, ILogger? logger = null)
    {
        return provider switch
        {
            ProviderType.OpenAI or ProviderType.AzureOpenAI => new OpenAIEmbeddingRequestValidator(),
            ProviderType.Anthropic => new AnthropicEmbeddingRequestValidator(),
            ProviderType.Google => new GoogleEmbeddingRequestValidator(),
            ProviderType.Ollama => new OllamaEmbeddingRequestValidator(),
            _ => throw new NotSupportedException($"Provider '{provider?.ToString()}' is not supported for embeddings.")
        };
    }
}