using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class GoogleChatRequestValidator : ChatRequestValidatorBase, IChatRequestValidator
{
    protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
    {
        // Google does not support ResponseFormat or Seed.
        if (!string.IsNullOrWhiteSpace(request.ResponseFormat))
        {
            logger?.LogWarning("Google does not support ResponseFormat. This field will be ignored.");
        }
        if (request.Seed.HasValue)
        {
            logger?.LogWarning("Google does not support Seed. This field will be ignored.");
        }

        // Google supports a system instruction via SystemPrompt, already handled in builder.
        logger?.LogInformation("Google chat request validated successfully.");
    }
}