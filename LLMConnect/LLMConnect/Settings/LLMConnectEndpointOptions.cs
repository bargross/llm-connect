namespace LLMConnect.Settings;

/// <summary>
/// Configuration options for connecting to a specific LLM provider endpoint, including custom deserialization and streaming behavior.
/// </summary>
public class LLMConnectEndpointOptions
{
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
    /// if not set. Ignored if <see cref="Endpoint"/> is set.
    /// </summary>
    public int? OllamaPort { get; set; }

    /// <summary>Reserved for future provider-specific configuration.</summary>
    public Dictionary<string, object>? ExtraOptions { get; set; }
}
