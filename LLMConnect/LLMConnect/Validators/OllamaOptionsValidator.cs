using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal class OllamaOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options)
    {
        // No API key required

        if (options.OllamaPort.HasValue)
        {
            if (options.OllamaPort.Value < 1 || options.OllamaPort.Value > 65535)
                throw new ArgumentException($"Invalid port: {options.OllamaPort.Value}. Must be between 1 and 65535.", nameof(options.OllamaPort));
        }
    }
}