using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class LLMProviderFactory
{
    private readonly HttpClient _providedClient;
    private readonly LLMConnectClientOptions _options;
    private readonly ILogger<LLMProviderFactory>? _logger;

    public LLMProviderFactory(LLMConnectClientOptions options, HttpClient httpClient)
    {
        _providedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public LLMProviderFactory(LLMConnectClientOptions options, IHttpClientFactory httpClientFactory)
    {
        _providedClient = httpClientFactory?.CreateClient("LLMConnect") ?? throw new InvalidOperationException("Failed to create HttpClient from factory.");
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public (HttpClient, ILLMProvider) CreateProvider()
    {
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_options, _providedClient);

        switch (_options.Provider)
        {
            case ProviderType.OpenAI: return (configuredClient, new OpenAIProvider(configuredClient, _options));
            case ProviderType.Anthropic: return (configuredClient, new AnthropicProvider(configuredClient, _options));
            case ProviderType.Google: return (configuredClient, new GoogleProvider(configuredClient, _options));
            case ProviderType.Ollama: return (configuredClient, new OllamaProvider(configuredClient, _options));
            default:
            {
                var message = $"Provider '{_options.Provider}' is not supported.";

                _logger?.LogError(message);

                throw new NotSupportedException(message);
            }
        }
    }
}