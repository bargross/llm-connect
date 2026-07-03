using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class EndpointRegistry
{
    private static readonly Dictionary<ProviderType, string> _defaultChatEndpoints = new()
    {
        { ProviderType.OpenAI, "https://api.openai.com/v1/" }, // /chat/completions && embeddings
        { ProviderType.Anthropic, "https://api.anthropic.com/v1/" }, // embeddings not supported
        { ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/models/" }, // replacegenerateContent with embedContent for embeddings
        { ProviderType.Ollama, "http://localhost:{port}/api/" } // for embeddings replace chat with embeddings
    };

    public static string GetDefaultEndpoint(ProviderType provider, ILogger? logger)
    {
        if (_defaultChatEndpoints.TryGetValue(provider, out var endpoint))
            return endpoint;

        var message = $"Provider '{provider}' is not supported.";

        logger?.LogError(message);

        throw new NotSupportedException(message);
    }

    public static string GetEndpointParams(QueryType qType, ProviderType provider)
    {
        switch(qType)
        {
            case QueryType.Chat:
                return provider switch
                {
                    ProviderType.OpenAI => "chat/completions",
                    ProviderType.Anthropic => "messages",
                    ProviderType.Google => "{model}:generateContent",
                    ProviderType.Ollama => "chat",
                    _ => throw new NotSupportedException($"Provider '{provider}' is not supported for chat queries.")
                };
            case QueryType.Embeddings:
                return provider switch
                {
                    ProviderType.OpenAI => "embeddings",
                    ProviderType.Anthropic => throw new NotSupportedException($"Provider '{provider}' does not support embeddings queries."),
                    ProviderType.Google => "{model}:embedContent",
                    ProviderType.Ollama => "embeddings",
                    _ => throw new NotSupportedException($"Provider '{provider}' is not supported for embeddings queries.")
                };
        }

        throw new NotSupportedException($"Query type '{qType}' is not supported.");
    }
}