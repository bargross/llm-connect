using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class EndpointOptionsValidator
{
    public void Validate(LLMConnectEndpointOptions endpointOptions, ProviderType provider, ILogger? logger = null)
    {
        if (endpointOptions == null)
            throw new ArgumentNullException(nameof(endpointOptions));

        switch (provider)
        {
            case ProviderType.OpenAI:
            case ProviderType.Anthropic:
            case ProviderType.Google:
                // No endpoint options required – using internal defaults
                break;

            case ProviderType.AzureOpenAI:
                ValidateAzureOptions(endpointOptions, logger);
                break;

            case ProviderType.Ollama:
                ValidateOllamaOptions(endpointOptions, logger);
                break;

            default:
                throw new NotSupportedException($"Provider '{provider}' is not supported.");
        }
    }

    private static void ValidateAzureOptions(LLMConnectEndpointOptions options, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(options.AzureResourceName))
        {
            var error = "AzureResourceName is required when using AzureOpenAI.";
            logger?.LogError(error);

            throw new ArgumentException(error, nameof(options.AzureResourceName));
        }

        if (string.IsNullOrWhiteSpace(options.AzureDeploymentName))
        {
            var error = "AzureDeploymentName is required when using AzureOpenAI.";
            logger?.LogError(error);

            throw new ArgumentException(error, nameof(options.AzureDeploymentName));
        }

        if (string.IsNullOrWhiteSpace(options.AzureApiVersion))
        {
            var error = "AzureApiVersion is required when using AzureOpenAI.";
            logger?.LogError(error);

            throw new ArgumentException(error, nameof(options.AzureApiVersion));
        }
    }

    private static void ValidateOllamaOptions(LLMConnectEndpointOptions options, ILogger? logger)
    {
        var ollamaPort = options.OllamaPort ?? 11434; // Default port if not specified
        if (options.OllamaPort.HasValue && (ollamaPort < 1 || ollamaPort > 65535))
        {
            var error = $"OllamaPort must be between 1 and 65535. Value: {options.OllamaPort.Value}";
            logger?.LogError(error);
                
            throw new ArgumentException(error, nameof(options.OllamaPort));   
        }
    }
}