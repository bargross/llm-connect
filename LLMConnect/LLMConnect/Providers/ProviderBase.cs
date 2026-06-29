using LLMConnect.Exceptions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace LLMConnect
{
    internal abstract class ProviderBase
    {
        protected async Task<string> ExtractErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                // Try to parse as JSON; if it fails, use the raw body
                try
                {
                    var error = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorJson);
                    return error?.Error?.Message ?? $"HTTP error: {response.StatusCode}";
                }
                catch (JsonException)
                {
                    return $"HTTP error: {response.StatusCode} - {errorJson}";
                }
            }
            catch (Exception ex)
            {
                return $"HTTP error: {response.StatusCode} - Failed to read error body: {ex.Message}";
            }
        }

        protected async Task LogAndThrow(ProviderType providerType, HttpResponseMessage response, ILogger? logger, CancellationToken cancellationToken)
        {
            var provider = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(providerType.ToString());

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException(provider, errorMessage);

            logger?.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }
    }
}
