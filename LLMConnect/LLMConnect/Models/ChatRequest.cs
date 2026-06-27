using LLMConnect.Models;
using System.Text.Json.Serialization;

public class ChatRequest
{
    public List<Message> Messages { get; set; } = new();

    public string? SystemPrompt { get; set; }

    public float Temperature { get; set; } = 0.7f;

    public float TopP { get; set; } = 0.9f;

    public int MaxTokens { get; set; } = 1024;

    public string? Model { get; set; }

    public string? Provider { get; set; }

    // --- New common parameters ---
    public List<string>? StopSequences { get; set; }

    public float? FrequencyPenalty { get; set; }

    public float? PresencePenalty { get; set; }

    public string? ResponseFormat { get; set; } // "text" or "json_object"

    public int? Seed { get; set; }

    public string? User { get; set; }

    // TODO: Tools support for function calling (Phase 2)
    // public List<Tool>? Tools { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
