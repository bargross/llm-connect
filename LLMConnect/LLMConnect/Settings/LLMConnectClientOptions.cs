using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Settings;

/// <summary>
/// 
/// </summary>
public class LLMConnectClientOptions
{
    /// <summary>
    /// 
    /// </summary>
    public ProviderType Provider { get; set; } = ProviderType.OpenAI;

    /// <summary>
    /// 
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public int? OllamaPort { get; set; } 

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Optional logger factory. If provided, logs will be emitted.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, object>? ExtraOptions { get; set; }
}