using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal abstract class ChatRequestValidatorBase
{
    public void Validate(ChatRequest request, ILogger? logger)
    {
        // Common validation
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Messages == null || request.Messages.Count == 0)
        {
            logger?.LogError("At least one message is required.");
            
            throw new ArgumentException("At least one message is required.", nameof(request.Messages));
        }

        if (request.Temperature < 0 || request.Temperature > 1)
        {
            logger?.LogError("Temperature must be between 0 and 1. Current value: {Temperature}", request.Temperature);
            
            throw new ArgumentException("Temperature must be between 0 and 1.", nameof(request.Temperature));
        }

        if (request.MaxTokens < 1)
        {
            logger?.LogError("MaxTokens must be greater than 0. Current value: {MaxTokens}", request.MaxTokens);
            
            throw new ArgumentException("MaxTokens must be greater than 0.", nameof(request.MaxTokens));
        }

        if (request.StopSequences != null && request.StopSequences.Any(string.IsNullOrWhiteSpace))
        {
            logger?.LogError("StopSequences cannot contain empty or whitespace strings.");
            
            throw new ArgumentException("StopSequences cannot contain empty or whitespace strings.", nameof(request.StopSequences));
        }

        // Provider-specific validation
        ValidateProviderSpecific(request, logger);
    }

    protected abstract void ValidateProviderSpecific(ChatRequest request, ILogger? logger);
}