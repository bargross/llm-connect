using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class LLMProviderFactory
{
    private readonly HttpClient _providedClient;
    private readonly LLMConnectGeneralOptions _generalOpts;
    private readonly LLMConnectEndpointOptions _endpointOpts;
    private readonly ILogger<LLMProviderFactory>? _logger;

    public LLMProviderFactory(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, HttpClient httpClient)
    {
        _generalOpts = generalOpts ?? throw new ArgumentNullException(nameof(generalOpts));
        _endpointOpts = endpointOpts ?? throw new ArgumentNullException(nameof(endpointOpts));
        _providedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _logger = generalOpts.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(generalOpts, endpointOpts, _logger);
    }

    public LLMProviderFactory(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, IHttpClientFactory httpClientFactory)
    {
        _generalOpts = generalOpts ?? throw new ArgumentNullException(nameof(generalOpts));
        _endpointOpts = endpointOpts ?? throw new ArgumentNullException(nameof(endpointOpts));
        _providedClient = httpClientFactory?.CreateClient("LLMConnect") ?? throw new InvalidOperationException("Failed to create HttpClient from factory.");

        _logger = generalOpts.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(generalOpts, endpointOpts, _logger);
    }

    public (HttpClient, ILLMProvider) CreateProvider()
    {
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_generalOpts, _endpointOpts, _providedClient);

        return _generalOpts.Provider switch
        {
            ProviderType.OpenAI => (configuredClient, new OpenAIProvider(configuredClient, _generalOpts, _endpointOpts)),
            ProviderType.Anthropic => (configuredClient, new AnthropicProvider(configuredClient, _generalOpts, _endpointOpts)),
            ProviderType.Google => (configuredClient, new GoogleProvider(configuredClient, _generalOpts, _endpointOpts)),
            ProviderType.Ollama => (configuredClient, new OllamaProvider(configuredClient, _generalOpts, _endpointOpts)),
        };
    }
}