using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleFunctionCallingConfig
{
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("allowed_function_names")]
    public List<string>? AllowedFunctionNames { get; set; }
}