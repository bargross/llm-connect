using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class OllamaChatRequestValidator : ChatRequestValidatorBase, IChatRequestValidator
{
    protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
    {
        // Ollama does not support ResponseFormat, Seed, FrequencyPenalty, PresencePenalty.
        if (!string.IsNullOrWhiteSpace(request.ResponseFormat))
        {
            logger?.LogWarning("Ollama does not support ResponseFormat. This field will be ignored.");
        }
        if (request.Seed.HasValue)
        {
            logger?.LogWarning("Ollama does not support Seed. This field will be ignored.");
        }
        if (request.FrequencyPenalty.HasValue)
        {
            logger?.LogWarning("Ollama does not support FrequencyPenalty. This field will be ignored.");
        }
        if (request.PresencePenalty.HasValue)
        {
            logger?.LogWarning("Ollama does not support PresencePenalty. This field will be ignored.");
        }

        logger?.LogInformation("Ollama chat request validated successfully.");
    }
}