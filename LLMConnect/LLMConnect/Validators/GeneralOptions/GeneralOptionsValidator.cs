using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal class GeneralOptionsValidator
{
    public virtual void Validate(LLMConnectGeneralOptions generalOptions, ILogger? logger = null)
    {
        if (generalOptions == null)
            throw new ArgumentNullException(nameof(generalOptions));

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
    }

    protected virtual void ValidateApiKey(LLMConnectGeneralOptions generalOptions, ILogger? logger = null)
    {
        switch (generalOptions.Provider)
        {
            case ProviderType.Ollama: break;
            case ProviderType.Anthropic:
            case ProviderType.OpenAI:
            case ProviderType.Google:
            case ProviderType.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(generalOptions.ApiKey))
                {
                    var errorMessage = $"Missing api key for provider {generalOptions.Provider.ToString()}";

                    logger?.LogError(errorMessage);
                    
                    throw new ArgumentException(errorMessage);
                }

                break;
        }
    }
}