using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class EndpointRegistry
{
    // Domain only (no version, no trailing slash)
    private static readonly Dictionary<ProviderType, string> _domains = new()
    {
        { ProviderType.OpenAI, "https://api.openai.com" },
        { ProviderType.Anthropic, "https://api.anthropic.com" },
        { ProviderType.Google, "https://generativelanguage.googleapis.com" },
        { ProviderType.Ollama, "http://localhost:{port}" }  // port placeholder
    };

    // API version for each provider (with leading slash)
    private static readonly Dictionary<ProviderType, string> _apiVersions = new()
    {
        { ProviderType.OpenAI, "/v1" },
        { ProviderType.Anthropic, "/v1" },
        { ProviderType.Google, "/v1beta" },
        { ProviderType.Ollama, "" }  // no version
    };

    // Relative paths (without leading slash) – for use after base + version
    private static readonly Dictionary<(ProviderType Provider, QueryType Query, bool IsStreaming), string> _relativePaths = new()
    {
        // OpenAI
        { (ProviderType.OpenAI, QueryType.Chat, false), "chat/completions" },
        { (ProviderType.OpenAI, QueryType.Chat, true),  "chat/completions" },   // stream flag in body
        { (ProviderType.OpenAI, QueryType.Embeddings, false), "embeddings" },

        // Anthropic
        { (ProviderType.Anthropic, QueryType.Chat, false), "messages" },
        { (ProviderType.Anthropic, QueryType.Chat, true),  "messages" },       // stream flag in body

        // Google
        { (ProviderType.Google, QueryType.Chat, false), "models/{model}:generateContent" },
        { (ProviderType.Google, QueryType.Chat, true),  "models/{model}:streamGenerateContent" },
        { (ProviderType.Google, QueryType.Embeddings, false), "models/{model}:embedContent" },

        // Ollama
        { (ProviderType.Ollama, QueryType.Chat, false), "api/chat" },
        { (ProviderType.Ollama, QueryType.Chat, true),  "api/chat" },         // stream flag in body
        { (ProviderType.Ollama, QueryType.Embeddings, false), "api/embed" }
    };

    /// <summary>
    /// Returns the default endpoint (base URL + API version) with a trailing slash.
    /// For Ollama, the {port} placeholder is replaced with the provided port.
    /// </summary>
    public static string GetDefaultEndpoint(ProviderType? provider, int? port = null, ILogger? logger = null)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (port == null) port = 11434;

        if (!_domains.TryGetValue(provider.Value, out var domain))
        {
            var message = $"Provider '{provider}' is not supported.";

            logger?.LogError(message);

            throw new NotSupportedException(message);
        }

        var version = _apiVersions.GetValueOrDefault(provider.Value, "");
        var baseUrl = $"{domain}{version}/";

        if (provider == ProviderType.Ollama)
            baseUrl = baseUrl.Replace("{port}", port?.ToString());
        
        return baseUrl;
    }

    /// <summary>
    /// Returns the relative path (no leading slash) for the given provider and query type.
    /// Placeholders like {model} are left for the caller to replace.
    /// </summary>
    public static string GetEndpointParams(ProviderType? provider, QueryType queryType, bool isStreaming, ILogger? logger = null)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));

        var key = (provider.Value, queryType, isStreaming);
        if (_relativePaths.TryGetValue(key, out var path))
            return path;

        var message = $"The combination Provider='{provider}', QueryType='{queryType}', IsStreaming='{isStreaming}' is not supported.";

        logger?.LogError(message);

        throw new NotSupportedException(message);
    }
}