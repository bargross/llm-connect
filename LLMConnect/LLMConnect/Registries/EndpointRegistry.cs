using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class EndpointRegistry
{
    // Domain templates (no version, no trailing slash)
    private static readonly Dictionary<ProviderType, string> _domains = new()
    {
        { ProviderType.OpenAI, "https://api.openai.com" },
        { ProviderType.Anthropic, "https://api.anthropic.com" },
        { ProviderType.Google, "https://generativelanguage.googleapis.com" },
        { ProviderType.Ollama, "http://localhost:{port}" },
        { ProviderType.AzureOpenAI, "https://{resource}.openai.azure.com/openai/deployments/{deployment}" }
    };

    // API version prefix (leading slash) for path‑based versions.
    // For Azure, it's empty because the version is a query param.
    private static readonly Dictionary<ProviderType, string> _apiVersions = new()
    {
        { ProviderType.OpenAI, "/v1" },
        { ProviderType.Anthropic, "/v1" },
        { ProviderType.Google, "/v1beta" },
        { ProviderType.Ollama, "" },
        { ProviderType.AzureOpenAI, "" }
    };

    // Relative paths (no leading slash) – may contain {model} placeholder.
    private static readonly Dictionary<(ProviderType Provider, QueryType Query, bool IsStreaming), string> _relativePaths = new()
    {
        // OpenAI
        { (ProviderType.OpenAI, QueryType.Chat, false), "chat/completions" },
        { (ProviderType.OpenAI, QueryType.Chat, true),  "chat/completions" },
        { (ProviderType.OpenAI, QueryType.Embeddings, false), "embeddings" },

        // Anthropic
        { (ProviderType.Anthropic, QueryType.Chat, false), "messages" },
        { (ProviderType.Anthropic, QueryType.Chat, true),  "messages" },

        // Google – contains {model} placeholder
        { (ProviderType.Google, QueryType.Chat, false), "models/{model}:generateContent" },
        { (ProviderType.Google, QueryType.Chat, true),  "models/{model}:streamGenerateContent" },
        { (ProviderType.Google, QueryType.Embeddings, false), "models/{model}:embedContent" },

        // Ollama
        { (ProviderType.Ollama, QueryType.Chat, false), "api/chat" },
        { (ProviderType.Ollama, QueryType.Chat, true),  "api/chat" },
        { (ProviderType.Ollama, QueryType.Embeddings, false), "api/embed" },

        // AzureOpenAI – same as OpenAI, no model placeholder
        { (ProviderType.AzureOpenAI, QueryType.Chat, false), "chat/completions" },
        { (ProviderType.AzureOpenAI, QueryType.Chat, true),  "chat/completions" },
        { (ProviderType.AzureOpenAI, QueryType.Embeddings, false), "embeddings" }
    };

    /// <summary>
    /// Returns the base address (domain + version) with a trailing slash.
    /// Placeholders {port}, {resource}, {deployment} are replaced.
    /// </summary>
    public static string GetDefaultEndpoint(
        ProviderType? provider,
        LLMConnectEndpointOptions endpointOptions,
        ILogger? logger = null)
    {
        if (provider == null)
        {
            var message = "Provider is not specified.";
            logger?.LogError(message);
        
            throw new ArgumentNullException(nameof(provider), message);
        }

        var providerValue = provider.Value;

        if (!_domains.TryGetValue(providerValue, out var domainTemplate))
        {
            var message = $"Provider '{provider}' is not supported.";
            logger?.LogError(message);
            throw new NotSupportedException(message);
        }

        var domain = domainTemplate
            .Replace("{resource}", endpointOptions.AzureResourceName ?? "")
            .Replace("{deployment}", endpointOptions.AzureDeploymentName ?? "")
            .Replace("{port}", endpointOptions.OllamaPort?.ToString() ?? "11434");

        var version = _apiVersions.GetValueOrDefault(providerValue, "");

        return $"{domain}{version}/";
    }

    /// <summary>
    /// Returns the relative path (no leading slash) for the given provider and query type.
    /// If a model is provided and the path contains {model}, it is replaced.
    /// </summary>
    public static string GetEndpointParams(
        ProviderType provider,
        QueryType queryType,
        bool isStreaming,
        string? model = null,
        ILogger? logger = null)
    {
        var key = (provider, queryType, isStreaming);
        if (!_relativePaths.TryGetValue(key, out var path))
        {
            var message = $"The combination Provider='{provider}', QueryType='{queryType}', IsStreaming='{isStreaming}' is not supported.";
            logger?.LogError(message);

            throw new NotSupportedException(message);
        }

        if (path.Contains("{model}") && !string.IsNullOrWhiteSpace(model))
            path = path.Replace("{model}", model);

        return path;
    }
}