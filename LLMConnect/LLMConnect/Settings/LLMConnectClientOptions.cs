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

    internal string InternalComputedDefaultModel =>
        !string.IsNullOrWhiteSpace(DefaultModel) ? DefaultModel
            : Provider switch
            {
                ProviderType.Ollama => "llama3.2",
                ProviderType.Google => "gemini-2.0-flash",
                ProviderType.Anthropic => "claude-3-5-sonnet-20241022",
                ProviderType.OpenAI => "gpt-3.5-turbo",
            };
}