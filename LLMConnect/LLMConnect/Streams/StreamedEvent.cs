namespace LLMConnect;

internal readonly record struct StreamEvent(string? EventName, string Data);