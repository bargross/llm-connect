using LLMConnect.Models;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;

internal static class OptionsValidatorFactory
{
    public static IOptionsValidator Create(ProviderType provider, ILogger? logger = null)
    {
        switch (provider)
        {
            case ProviderType.OpenAI: return new OpenAIOptionsValidator();
            case ProviderType.Anthropic: return new AnthropicOptionsValidator();
            case ProviderType.Google: return new GoogleOptionsValidator();
            case ProviderType.Ollama: return new OllamaOptionsValidator();
            default:
                {
                    var message = $"Provider '{provider.ToString()}' is not supported.";

                    logger?.LogError(message);

                    throw new NotSupportedException(message);
                }
        }
    }
}