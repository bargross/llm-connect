using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal abstract class OptionsValidationBase
{
    public virtual void Validate(LLMConnectGeneralOptions generalOptions, LLMConnectEndpointOptions endpointOptions, ILogger? logger = null)
    {
        if (generalOptions == null)
            throw new ArgumentNullException(nameof(generalOptions));

        if (endpointOptions == null)
            throw new ArgumentNullException(nameof(endpointOptions));

        if (generalOptions.Provider == null)
        {
            var errorMessage = "Provider must be specified.";
     
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(generalOptions.Provider));
        }

        ValidateApiKey(generalOptions, logger);

        if (generalOptions.Timeout <= TimeSpan.Zero)
        {
            var errorMessage = "Timeout must be greater than zero.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(generalOptions.Timeout));
        }

        if (generalOptions.MaxRetries < 0)
        {
            var errorMessage = $"MaxRetries must be >= 0.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(generalOptions.MaxRetries));
        }

        if (!string.IsNullOrWhiteSpace(generalOptions.DefaultModel) && generalOptions.DefaultModel.Length > 100)
        {
            var errorMessage = "DefaultModel cannot exceed 100 characters.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(generalOptions.DefaultModel));
        }

        ValidateEndpoint(endpointOptions, generalOptions.Provider, logger);

        ValidateProviderSpecificGeneralOptions(generalOptions, logger);

        ValidateProviderSpecificEndpointOptions(endpointOptions, logger);
    }

    protected virtual void ValidateEndpoint(LLMConnectEndpointOptions endpointOpts, ProviderType? provider, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(endpointOpts.Endpoint))
            return;

        if (!Uri.IsWellFormedUriString(endpointOpts.Endpoint, UriKind.Absolute))
        {
            var errorMessage = $"Invalid endpoint URL: {endpointOpts.Endpoint} for provider {provider?.ToString()}";

            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(endpointOpts.Endpoint));
        }

        var genericProviderEndpoint = new Uri(endpointOpts.Endpoint);
        if (provider != ProviderType.Ollama &&
            genericProviderEndpoint.Scheme != Uri.UriSchemeHttps &&
            genericProviderEndpoint.Host != "localhost" &&
            genericProviderEndpoint.Host != "127.0.0.1")
        {
            var errorMessage = $"Endpoint must use HTTPS for provider '{provider?.ToString()}'.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(endpointOpts.Endpoint));
        }

        // Optional: Warn about known mismatches
        var openAIUri = new Uri(endpointOpts.Endpoint);
        if (provider == ProviderType.OpenAI &&
            !openAIUri.Host.Contains("openai.com") &&
            !openAIUri.Host.Contains("azure.com") &&
            !openAIUri.Host.Contains("localhost"))
        {
            logger?.LogWarning("OpenAI provider used with non-OpenAI endpoint.");

            // Log a warning — but don't throw
            System.Diagnostics.Debug.WriteLine("Warning: OpenAI provider used with non-OpenAI endpoint.");
        }
    }

    protected virtual void ValidateApiKey(LLMConnectGeneralOptions generalOptions, ILogger? logger = null)
    {
        switch (generalOptions.Provider)
        {
            case ProviderType.Ollama: break;
            case ProviderType.Anthropic:
            case ProviderType.OpenAI:
            case ProviderType.Google:
                if (string.IsNullOrWhiteSpace(generalOptions.ApiKey))
                {
                    var errorMessage = $"Missing api key for provider {generalOptions.Provider.ToString()}";

                    logger?.LogError(errorMessage);
                    
                    throw new ArgumentException(errorMessage);
                }

                break;
        }
    }

    protected abstract void ValidateProviderSpecificGeneralOptions(LLMConnectGeneralOptions generalOptions, ILogger? logger = null);

    protected abstract void ValidateProviderSpecificEndpointOptions(LLMConnectEndpointOptions endpointOptions, ILogger? logger = null);
}