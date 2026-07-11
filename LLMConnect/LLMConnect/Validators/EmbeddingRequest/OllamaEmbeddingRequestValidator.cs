using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class OllamaEmbeddingRequestValidator : EmbeddingRequestValidatorBase
{
    protected override void ValidateProviderSpecific(EmbeddingRequest request, ILogger? logger)
    {
        // Ollama does not have additional mandatory constraints, but we can log a warning
        // if extra parameters are present (they will be passed as options anyway).

        if (request.ExtraParameters != null && request.ExtraParameters.Count > 0)
        {
            logger?.LogInformation("Ollama embedding request contains extra parameters: {Count}", request.ExtraParameters.Count);
        }

        logger?.LogInformation("Ollama embedding request validated successfully.");
    }
}