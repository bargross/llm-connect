using LLMConnect.Models;
using LLMConnect.Settings;
using System.Net.Http.Headers;

namespace LLMConnect;

internal static class HttpClientConfigurator
{
    internal static HttpClient ConfigureForProvider(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, HttpClient client)
    {
        // Build the base address from the registry
        var endpoint = EndpointRegistry.GetDefaultEndpoint(
            generalOpts.Provider,
            endpointOpts,
            logger: generalOpts.LoggerFactory?.CreateLogger("EndpointRegistry"));

        client.BaseAddress = new Uri(endpoint);
        client.Timeout = generalOpts.Timeout;

        // Remove headers we control
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove("Accept");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Remove("anthropic-version");
        client.DefaultRequestHeaders.Remove("x-goog-api-key");

        switch (generalOpts.Provider)
        {
            case ProviderType.OpenAI:
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {generalOpts.ApiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                break;

            case ProviderType.AzureOpenAI:
                // Azure uses the "api-key" header
                client.DefaultRequestHeaders.Add("api-key", generalOpts.ApiKey);
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

        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("LLMConnect", "1.0.0"));
        }

        return client;
    }
}