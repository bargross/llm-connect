using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect
{
    internal static class ChatRequestValidatorFactory
    {
        public static IChatRequestValidator Create(ProviderType provider, ILogger? logger = null)
        {
            switch (provider)
            {
                case ProviderType.OpenAI: return new OpenAIChatRequestValidator();
                case ProviderType.Anthropic: return new AnthropicChatRequestValidator();
                case ProviderType.Google: return new GoogleChatRequestValidator();
                case ProviderType.Ollama: return new OllamaChatRequestValidator();
                default:
                    {
                        var message = $"Provider '{provider.ToString()}' is not supported.";

                        logger?.LogError(message);

                        throw new NotSupportedException(message);
                    }
            }
        }
    }
}
