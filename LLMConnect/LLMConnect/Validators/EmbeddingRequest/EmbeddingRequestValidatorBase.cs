using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal abstract class EmbeddingRequestValidatorBase: IEmbeddingRequestValidator
{
    public void Validate(EmbeddingRequest request, ILogger? logger)
    {
        // Common validation
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            logger?.LogError("Embedding text cannot be null or whitespace.");
            throw new ArgumentException("Embedding text cannot be null or whitespace.", nameof(request.Text));
        }

        if (!string.IsNullOrEmpty(request.Model) && request.Model.Length > 100)
        {
            logger?.LogError("Model name exceeds 100 characters.");
            throw new ArgumentException("Model name cannot exceed 100 characters.", nameof(request.Model));
        }

        if (request.Dimensions.HasValue && request.Dimensions.Value < 1)
        {
            logger?.LogError("Dimensions must be a positive integer.");
            throw new ArgumentException("Dimensions must be a positive integer.", nameof(request.Dimensions));
        }

        // Provider-specific validation
        ValidateProviderSpecific(request, logger);
    }

    protected abstract void ValidateProviderSpecific(EmbeddingRequest request, ILogger? logger);
}