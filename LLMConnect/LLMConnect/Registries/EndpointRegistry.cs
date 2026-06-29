using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class EndpointRegistry
{
    private static readonly Dictionary<ProviderType, string> _defaultEndpoints = new()
    {
        { ProviderType.OpenAI, "https://api.openai.com/v1/chat/completions" },
        { ProviderType.Anthropic, "https://api.anthropic.com/v1/messages" },
        { ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent" },
        { ProviderType.Ollama, "http://localhost:{port}/api/chat" }
    };

    public static string GetDefaultEndpoint(ProviderType provider, ILogger? logger)
    {
        if (_defaultEndpoints.TryGetValue(provider, out var endpoint))
            return endpoint;

        var message = $"Provider '{provider}' does not have a default endpoint.";

        logger?.LogError(message);

        throw new NotSupportedException(message);
    }
}