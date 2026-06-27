namespace LLMConnect.Exceptions;

public class LLMConnectException : Exception
{
    public string? Provider { get; set; }

    public LLMConnectException(string message) : base(message) { }

    public LLMConnectException(string message, Exception innerException) : base(message, innerException) { }

    public LLMConnectException(string provider, string message) : base(message)
    {
        Provider = provider;
    }

    public LLMConnectException(string provider, string message, Exception innerException) : base(message, innerException)
    {
        Provider = provider;
    }
}