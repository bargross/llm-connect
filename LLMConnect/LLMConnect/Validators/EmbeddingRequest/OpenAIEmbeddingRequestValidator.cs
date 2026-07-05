using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class OpenAIEmbeddingRequestValidator : EmbeddingRequestValidatorBase
{
    protected override void ValidateProviderSpecific(EmbeddingRequest request, ILogger? logger)
    {
        // OpenAI supports "float" or "base64" encoding format
        if (request.EncodingFormat != "float" && request.EncodingFormat != "base64")
        {
            logger?.LogError("EncodingFormat must be 'float' or 'base64'.");
            
            throw new ArgumentException("EncodingFormat must be 'float' or 'base64'.", nameof(request.EncodingFormat));
        }

        // User is optional, but if provided it must not be empty
        if (request.User != null && string.IsNullOrWhiteSpace(request.User))
        {
            logger?.LogError("User identifier cannot be whitespace.");
            throw new ArgumentException("User identifier cannot be whitespace.", nameof(request.User));
        }

        logger?.LogInformation("OpenAI embedding request validated successfully.");
    }
}