using LLMConnect.Models;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a request to generate a chat completion, including the
/// conversation history and provider-agnostic generation parameters.
/// </summary>
public class ChatRequest
{
    /// <summary>The conversation history to send to the model, in chronological order.</summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// An optional system instruction. If both this and a <see cref="SystemMessage"/>
    /// in <see cref="Messages"/> are set, provider-specific behavior determines which
    /// takes precedence.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Controls randomness in generation. Higher values produce more varied output.</summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>The nucleus sampling threshold, restricting generation to the smallest set of tokens whose cumulative probability exceeds this value.</summary>
    public float TopP { get; set; } = 0.9f;

    /// <summary>The maximum number of tokens to generate in the response.</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// The model to use for this request. If not set, falls back to
    /// <see cref="LLMConnect.Settings.LLMConnectClientOptions.DefaultModel"/>.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Reserved for future use. Currently the provider is determined by
    /// <see cref="LLMConnect.Settings.LLMConnectClientOptions.Provider"/>, not by this property.
    /// </summary>
    //public string? Provider { get; set; }

    /// <summary>One or more sequences that, if generated, will cause the model to stop producing further tokens.</summary>
    public List<string>? StopSequences { get; set; }

    /// <summary>Penalizes tokens based on how frequently they have already appeared in the generated text, reducing repetition. OpenAI-specific.</summary>
    public float? FrequencyPenalty { get; set; }

    /// <summary>Penalizes tokens that have already appeared at all in the generated text, regardless of frequency. OpenAI-specific.</summary>
    public float? PresencePenalty { get; set; }

    /// <summary>The desired output format, e.g. <c>"text"</c> or <c>"json_object"</c>. Not supported by every provider.</summary>
    public string? ResponseFormat { get; set; } // "text" or "json_object"

    /// <summary>An optional seed for more deterministic generation. Not supported by every provider.</summary>
    public int? Seed { get; set; }

    /// <summary>An optional, opaque identifier representing the end user, used by some providers for abuse monitoring.</summary>
    public string? User { get; set; }

    // TODO: Tools support for function calling (Phase 2)
    // public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Additional, provider-specific request properties not otherwise modeled by
    /// this type. Serialized as extra top-level JSON properties on the outgoing request.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
