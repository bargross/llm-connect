namespace LLMConnect;

internal class AnthropicToolCallState
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string AccumulatedArguments { get; set; } = string.Empty;
}
