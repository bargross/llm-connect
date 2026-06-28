using LLMConnect.Models;

namespace LLMConnect.Settings;

public class LLMConnectClientOptions
{
    public ProviderType Provider { get; set; } = ProviderType.OpenAI;

    public string ApiKey { get; set; } = string.Empty;

    public string? DefaultModel { get; set; }

    public string? Endpoint { get; set; }

    public int? OllamaPort { get; set; } 

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    public int MaxRetries { get; set; } = 3;

    public Dictionary<string, object>? ExtraOptions { get; set; }
}