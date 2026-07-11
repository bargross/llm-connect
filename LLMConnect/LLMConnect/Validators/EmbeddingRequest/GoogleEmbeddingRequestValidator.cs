using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class GoogleEmbeddingRequestValidator : EmbeddingRequestValidatorBase
{
    protected override void ValidateProviderSpecific(EmbeddingRequest request, ILogger? logger)
    {
        // Google supports optional taskType: RETRIEVAL_DOCUMENT, RETRIEVAL_QUERY, etc.
        if (!string.IsNullOrEmpty(request.TaskType))
        {
            var validTaskTypes = new[] { "RETRIEVAL_DOCUMENT", "RETRIEVAL_QUERY", "CLASSIFICATION", "CLUSTERING", "SEMANTIC_SIMILARITY" };
            if (!validTaskTypes.Contains(request.TaskType))
            {
                logger?.LogError("TaskType must be one of: {ValidTypes}", string.Join(", ", validTaskTypes));
                throw new ArgumentException($"TaskType must be one of: {string.Join(", ", validTaskTypes)}.", nameof(request.TaskType));
            }
        }

        // Title is optional but if provided, must not be empty
        if (request.Title != null && string.IsNullOrWhiteSpace(request.Title))
        {
            logger?.LogError("Title cannot be whitespace.");
            throw new ArgumentException("Title cannot be whitespace.", nameof(request.Title));
        }

        // Role is optional, but if provided must be "user" or "model"
        if (request.Role != null && request.Role != "user" && request.Role != "model")
        {
            logger?.LogError("Role must be 'user' or 'model'.");
            throw new ArgumentException("Role must be 'user' or 'model'.", nameof(request.Role));
        }

        logger?.LogInformation("Google embedding request validated successfully.");
    }
}