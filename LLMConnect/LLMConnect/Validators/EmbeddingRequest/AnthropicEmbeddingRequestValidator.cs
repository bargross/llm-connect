using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class AnthropicEmbeddingRequestValidator : IEmbeddingRequestValidator
{
    public void Validate(EmbeddingRequest request, ILogger? logger)
    {
        // Anthropic does not have additional mandatory constraints, but we can log a warning
        // if extra parameters are present (they will be passed as options anyway).
        var message = "Anthropic does not support embedding generation.";

        logger?.LogError(message);

        throw new NotSupportedException(message);
    }
}