using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class OpenAIChatRequestValidator : ChatRequestValidatorBase, IChatRequestValidator
{
    protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
    {
        // OpenAI supports ResponseFormat and Seed
        if (!string.IsNullOrWhiteSpace(request.ResponseFormat) &&
            request.ResponseFormat != "text" &&
            request.ResponseFormat != "json_object")
        {
            logger?.LogError("ResponseFormat must be 'text' or 'json_object'. Current value: {ResponseFormat}", request.ResponseFormat);

            throw new ArgumentException("ResponseFormat must be 'text' or 'json_object'.", nameof(request.ResponseFormat));
        }

        if (request.Seed.HasValue && request.Seed < 0)
        {
            logger?.LogError("Seed must be a non-negative integer. Current value: {Seed}", request.Seed);

            throw new ArgumentException("Seed must be a non-negative integer.", nameof(request.Seed));
        }

        logger?.LogInformation("OpenAI chat request validated successfully.");
    }
}