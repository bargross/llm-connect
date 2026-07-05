using LLMConnect.Models;
using LLMConnect.Settings;
using System.Net.Http.Headers;

namespace LLMConnect;

internal static class HttpClientConfigurator
{
    internal static HttpClient ConfigureForProvider(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, HttpClient client)
    {
        var endpoint = ResolveEndpoint(endpointOpts?.Endpoint, endpointOpts?.OllamaPort, generalOpts);

        client.BaseAddress = new Uri(endpoint);
        client.Timeout = generalOpts.Timeout;

        // Remove only the headers we will set, so user-provided headers (like User-Agent) are preserved
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove("Accept");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Remove("anthropic-version");
        client.DefaultRequestHeaders.Remove("x-goog-api-key");

        switch (generalOpts.Provider)
        {
            case ProviderType.OpenAI:
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {generalOpts.ApiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Anthropic:
                client.DefaultRequestHeaders.Add("x-api-key", generalOpts.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Google:
                client.DefaultRequestHeaders.Add("x-goog-api-key", generalOpts.ApiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.Ollama:
                // No authentication required
                break;

            default:
                throw new NotSupportedException($"Provider '{generalOpts.Provider}' is not supported.");
        }

        // Only add the default User-Agent if the user hasn't set one already
        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("LLMConnect", "1.0.0"));
        }

        return client;
    }

    private static string ResolveEndpoint(string? optionsEndpoint, int? ollamaPort, LLMConnectGeneralOptions options)
    {
        if (!string.IsNullOrWhiteSpace(optionsEndpoint))
            return optionsEndpoint;

        var endpoint = EndpointRegistry.GetDefaultEndpoint(options.Provider, ollamaPort, options.LoggerFactory?.CreateLogger("EndpointRegistry"));

        if (options.Provider == ProviderType.Ollama)
        {
            var port = ollamaPort?.ToString() ?? "11434";
            endpoint = endpoint.Replace("{port}", port);
        }

        return endpoint;
    }
}