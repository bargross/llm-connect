using LLMConnect.Models;
using LLMConnect.Settings;
using System.Net.Http.Headers;

namespace LLMConnect;

internal static class HttpClientConfigurator
{
    internal static HttpClient ConfigureForProvider(LLMConnectClientOptions options, HttpClient client)
    {
        var endpoint = ResolveEndpoint(options);

        client.BaseAddress = new Uri(endpoint);
        client.Timeout = options.Timeout;

        // Remove only the headers we will set, so user-provided headers (like User-Agent) are preserved
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove("Accept");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Remove("anthropic-version");
        client.DefaultRequestHeaders.Remove("x-goog-api-key");

        switch (options.Provider)
        {
            case ProviderType.OpenAI:
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Anthropic:
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Google:
                client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Ollama:
                // No authentication required
                break;

            default:
                throw new NotSupportedException($"Provider '{options.Provider}' is not supported.");
        }

        // Only add the default User-Agent if the user hasn't set one already
        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("LLMConnect", "1.0.0"));
        }

        return client;
    }

    private static string ResolveEndpoint(LLMConnectClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
            return options.Endpoint;

        var endpoint = EndpointRegistry.GetDefaultEndpoint(options.Provider, options.LoggerFactory?.CreateLogger("EndpointRegistry"));

        if (options.Provider == ProviderType.Ollama)
        {
            var port = options.OllamaPort?.ToString() ?? "11434";
            endpoint = endpoint.Replace("{port}", port);
        }

        return endpoint;
    }
}