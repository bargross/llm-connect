namespace LLMConnect;

/// <summary>
/// Represents a streamed event with an optional event name and associated data.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="Data">The data associated with the event.</param>
public readonly record struct StreamEvent(string? EventName, string Data);