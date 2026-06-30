namespace LLMConnect.Exceptions;

/// <summary>
/// The exception thrown when a request to an LLM provider fails, whether due to
/// a non-success HTTP response, a network failure, or an unexpected error while
/// processing the request or response.
/// </summary>
public class LLMConnectException : Exception
{
    /// <summary>
    /// The name of the provider that raised this error (e.g. "OpenAI", "Anthropic",
    /// "Google", "Ollama"), or <see langword="null"/> if the error originated outside
    /// a specific provider call.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// </summary>
    public LLMConnectException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with the specified error message and the
    /// underlying exception that caused it.
    /// </summary>
    public LLMConnectException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with the specified error message and the
    /// name of the provider that raised it.
    /// </summary>
    public LLMConnectException(string provider, string message) : base(message)
    {
        Provider = provider;
    }

    /// <summary>
    /// Initializes a new instance with the specified error message, the
    /// name of the provider that raised it, and the underlying exception
    /// that caused it.
    /// </summary>
    public LLMConnectException(string provider, string message, Exception innerException) : base(message, innerException)
    {
        Provider = provider;
    }
}