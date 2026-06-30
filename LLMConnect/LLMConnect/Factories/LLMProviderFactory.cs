using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LLMConnect;

internal class LLMProviderFactory
{
    private readonly HttpClient _providedClient;
    private readonly LLMConnectClientOptions _options;
    private readonly ILogger<LLMProviderFactory>? _logger;

    public LLMProviderFactory(LLMConnectClientOptions options, HttpClient httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _providedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public LLMProviderFactory(LLMConnectClientOptions options, IHttpClientFactory httpClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _providedClient = httpClientFactory?.CreateClient("LLMConnect") ?? throw new InvalidOperationException("Failed to create HttpClient from factory.");

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public (HttpClient, ILLMProvider) CreateProvider()
    {
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_options, _providedClient);

        return _options.Provider switch
        {
            ProviderType.OpenAI => (configuredClient, new OpenAIProvider(configuredClient, _options)),
            ProviderType.Anthropic => (configuredClient, new AnthropicProvider(configuredClient, _options)),
            ProviderType.Google => (configuredClient, new GoogleProvider(configuredClient, _options)),
            ProviderType.Ollama => (configuredClient, new OllamaProvider(configuredClient, _options)),
        };
    }
}