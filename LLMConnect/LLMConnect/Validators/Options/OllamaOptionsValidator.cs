using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal class OllamaOptionsValidator : OptionsValidationBase, IOptionsValidator
{
    protected override void ValidateProviderSpecificGeneralOptions(LLMConnectGeneralOptions options, ILogger? logger = null)
    {
        // No API key required
    }

    protected override void ValidateProviderSpecificEndpointOptions(LLMConnectEndpointOptions options, ILogger? logger = null)
    {
        // No API key required

        if (options.OllamaPort.HasValue)
        {
            if (options.OllamaPort.Value < 1 || options.OllamaPort.Value > 65535)
            {
                var errorMessage = $"Invalid port: {options.OllamaPort.Value}. Must be between 1 and 65535.";

                logger?.LogError(errorMessage);

                throw new ArgumentException(errorMessage, nameof(options.OllamaPort));
            }
        }
    }
}