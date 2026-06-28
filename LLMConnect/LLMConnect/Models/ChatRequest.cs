using LLMConnect.Models;
using System.Text.Json.Serialization;

/// <summary>
/// 
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// 
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// 
    /// </summary>
    public float TopP { get; set; } = 0.9f;

    /// <summary>
    /// 
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// 
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<string>? StopSequences { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float? FrequencyPenalty { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float? PresencePenalty { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? ResponseFormat { get; set; } // "text" or "json_object"

    /// <summary>
    /// 
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? User { get; set; }

    // TODO: Tools support for function calling (Phase 2)
    // public List<Tool>? Tools { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
