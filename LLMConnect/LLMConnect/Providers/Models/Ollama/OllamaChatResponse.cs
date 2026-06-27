using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }
}