using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Settings;

/// <summary>
/// Configuration options for an <see cref="LLMConnect.LLMConnectClient"/> instance,
/// including which provider to use, credentials, and request behavior.
/// </summary>
public class LLMConnectClientOptions
{
    /// <summary>The LLM provider to target.</summary>
    public ProviderType Provider { get; set; } = ProviderType.OpenAI;

    /// <summary>The API key for the configured provider. Not required when <see cref="Provider"/> is <see cref="ProviderType.Ollama"/>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The model used for a request when <see cref="ChatRequest.Model"/> is not set.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// The name of the Azure resource (e.g., "my-azure-openai-resource") to use for requests.
    /// </summary>
    public string? AzureResourceName { get; set; }

    /// <summary>
    /// The name of the Azure deployment (e.g., "my-deployment") to use for requests.
    /// </summary>
    public string? AzureDeploymentName { get; set; }

    /// <summary>
    /// The API version to use for Azure OpenAI requests (e.g., "2023-06-01-preview").
    /// </summary>
    public string? AzureApiVersion { get; set; }

    /// <summary>
    /// The port a local Ollama server is listening on. Defaults to <c>11434</c>
    /// if not set, default is applied.
    /// </summary>
    public int? OllamaPort { get; set; }

    /// <summary>The per-request HTTP timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>The maximum number of retry attempts for transient failures. Must be zero or greater; <c>0</c> disables retries.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Optional logger factory. If provided, logs will be emitted.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>Reserved for future provider-specific configuration.</summary>
    public Dictionary<string, object>? ExtraOptions { get; set; }
}