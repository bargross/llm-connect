using LLMConnect.Models;
using LLMConnect.Validators.Options;

internal static class OptionsValidatorFactory
{
    public static IOptionsValidator Create(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.OpenAI => new OpenAIOptionsValidator(),
            ProviderType.Anthropic => new AnthropicOptionsValidator(),
            ProviderType.Google => new GoogleOptionsValidator(),
            ProviderType.Ollama => new OllamaOptionsValidator(),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
    }
}