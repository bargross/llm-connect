using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal abstract class LLMConnectOptionsValidationBase
{
    public virtual void Validate(LLMConnectClientOptions options, ILogger? logger = null)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        ValidateApiKey(options, logger);

        if (options.Timeout <= TimeSpan.Zero)
        {
            var errorMessage = "Timeout must be greater than zero.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(options.Timeout));
        }

        if (options.MaxRetries < 0)
        {
            var errorMessage = $"MaxRetries must be >= 0.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(options.MaxRetries));
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultModel) && options.DefaultModel.Length > 100)
        {
            var errorMessage = "DefaultModel cannot exceed 100 characters.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(options.DefaultModel));
        }

        ValidateEndpoint(options, logger);

        ValidateProviderSpecific(options, logger);
    }

    protected virtual void ValidateEndpoint(LLMConnectClientOptions options, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            return;

        if (!Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
        {
            var errorMessage = $"Invalid endpoint URL: {options.Endpoint} for provider {options.Provider.ToString()}";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(options.Endpoint));
        }

        var genericProviderEndpoint = new Uri(options.Endpoint);
        if (options.Provider != ProviderType.Ollama &&
            genericProviderEndpoint.Scheme != Uri.UriSchemeHttps &&
            genericProviderEndpoint.Host != "localhost" &&
            genericProviderEndpoint.Host != "127.0.0.1")
        {
            var errorMessage = $"Endpoint must use HTTPS for provider '{options.Provider.ToString()}'.";
            logger?.LogError(errorMessage);

            throw new ArgumentException(errorMessage, nameof(options.Endpoint));
        }

        // Optional: Warn about known mismatches
        var openAIUri = new Uri(options.Endpoint);
        if (options.Provider == ProviderType.OpenAI &&
            !openAIUri.Host.Contains("openai.com") &&
            !openAIUri.Host.Contains("azure.com") &&
            !openAIUri.Host.Contains("localhost"))
        {
            logger?.LogWarning("OpenAI provider used with non-OpenAI endpoint.");

            // Log a warning — but don't throw
            System.Diagnostics.Debug.WriteLine("Warning: OpenAI provider used with non-OpenAI endpoint.");
        }
    }

    protected virtual void ValidateApiKey(LLMConnectClientOptions options, ILogger? logger = null)
    {
        switch (options.Provider)
        {
            case ProviderType.Ollama: break;
            case ProviderType.Anthropic:
            case ProviderType.OpenAI:
            case ProviderType.Google:
                var errorMessage = $"Missing api key for provider {options.Provider.ToString()}";
                logger?.LogError(errorMessage);

                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    throw new ArgumentException(errorMessage);

                break;
        }
    }

    protected abstract void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger = null);
}