using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class LLMProviderFactory : ILLMProviderFactory
{
    private readonly HttpClient? _providedClient;
    private readonly LLMConnectClientOptions _options;
    private readonly IHttpClientFactory? _httpClientFactory;
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
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public (HttpClient, ILLMProvider) CreateProvider()
    {
        var httpClient = GetOrCreateHttpClient();

        switch (_options.Provider)
        {
            case ProviderType.OpenAI: return (httpClient, new OpenAIProvider(httpClient, _options));
            case ProviderType.Anthropic: return (httpClient, new AnthropicProvider(httpClient, _options));
            case ProviderType.Google: return (httpClient, new GoogleProvider(httpClient, _options));
            case ProviderType.Ollama: return (httpClient, new OllamaProvider(httpClient, _options));
            default:
            {
                var message = $"Provider '{_options.Provider}' is not supported.";

                _logger?.LogError(message);

                throw new NotSupportedException(message);
            }
        }
    }

    private HttpClient GetOrCreateHttpClient()
    {
        var client = null as HttpClient;
        if (_providedClient != null)
        {
            _logger?.LogWarning(
                "Using user-provided HttpClient. Retry logic must be configured by the caller. " +
                "Consider using the constructor that accepts IHttpClientFactory for automatic retries.");

            client = _providedClient; // user manages retry logic
        }
        else if (_httpClientFactory != null)
        {
            var factoryClient = _httpClientFactory.CreateClient("LLMConnect");

            _logger?.LogInformation(
                "Using IHttpClientFactory. Retry handler should be applied via AddHttpMessageHandler.");

            client = factoryClient; // factory manages retry logic
        }
        else
        {
            _logger?.LogInformation(
                "Creating new HttpClient with retry handler (MaxRetries = {MaxRetries}).",
                _options.MaxRetries);
        
            client = new HttpClient(new RetryDelegatingHandler(_options.MaxRetries));
        }


        return HttpClientConfigurator.ConfigureForProvider(_options, client);
    }
}