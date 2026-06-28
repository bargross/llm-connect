namespace LLMConnect.Exceptions;

/// <summary>
/// 
/// </summary>
public class LLMConnectException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public LLMConnectException(string message) : base(message) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public LLMConnectException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message"></param>
    public LLMConnectException(string provider, string message) : base(message)
    {
        Provider = provider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public LLMConnectException(string provider, string message, Exception innerException) : base(message, innerException)
    {
        Provider = provider;
    }
}