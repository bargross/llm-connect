using LLMConnect.Models;

namespace LLMConnect;

/// <summary>
/// Defines a provider-agnostic client for sending chat completion requests
/// to an LLM, either as a single response or as a stream of incremental chunks.
/// </summary>
public interface ILLMConnectClient
{
    /// <summary>
    /// Sends a chat completion request and returns the complete response once
    /// the model has finished generating.
    /// </summary>
    /// <param name="request">The chat request, including the conversation history and generation parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The completed chat response, or <see langword="null"/> if no response was returned.</returns>
    /// <exception cref="LLMConnect.Exceptions.LLMConnectException">Thrown when the provider returns an error or the request otherwise fails.</exception>
    Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat completion request and streams the response as a sequence
    /// of incremental <see cref="ChatChunk"/> values as they arrive from the provider.
    /// </summary>
    /// <param name="request">The chat request, including the conversation history and generation parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the stream.</param>
    /// <returns>An asynchronous sequence of response chunks, ending with a chunk where <see cref="ChatChunk.IsComplete"/> is <see langword="true"/>.</returns>
    /// <exception cref="LLMConnect.Exceptions.LLMConnectException">Thrown when the provider returns an error or the request otherwise fails.</exception>
    IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}