using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleTool
{
    [JsonPropertyName("functionDeclarations")]
    public List<GoogleFunctionDeclaration> FunctionDeclarations { get; set; } = new();
}