using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class AnthropicChatRequestValidator : ChatRequestValidatorBase, IChatRequestValidator
{
    protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
    {
        // Anthropic does not support ResponseFormat or Seed natively.
        // If they are provided, we can log a warning but not block.
        if (!string.IsNullOrWhiteSpace(request.ResponseFormat))
        {
            logger?.LogWarning("Anthropic does not support ResponseFormat. This field will be ignored.");
        }
        if (request.Seed.HasValue)
        {
            logger?.LogWarning("Anthropic does not support Seed. This field will be ignored.");
        }

        logger?.LogInformation("Anthropic chat request validated successfully.");
    }
}