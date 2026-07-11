using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleToolConfig
{
    [JsonPropertyName("function_calling_config")]
    public GoogleFunctionCallingConfig? FunctionCallingConfig { get; set; }
}