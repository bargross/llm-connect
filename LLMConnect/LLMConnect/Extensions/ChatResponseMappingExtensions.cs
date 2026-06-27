using LLMConnect.Models;
using System.Text.Json;

namespace LLMConnect;

internal static class ChatResponseMappingExtensions
{
    internal static ChatResponse ToChatResponse(this OpenAIChatResponse response)
    {
        if (response?.Choices?.FirstOrDefault()?.Message is not OpenAIResponseMessage message)
        {
            return new ChatResponse
            {
                Content = string.Empty,
                Usage = new Usage()
            };
        }

        return new ChatResponse
        {
            Content = message.Content ?? string.Empty,
            FinishReason = response.Choices.FirstOrDefault()?.FinishReason,
            Usage = new Usage
            {
                InputTokens = response.Usage?.PromptTokens ?? 0,
                OutputTokens = response.Usage?.CompletionTokens ?? 0
            },
            Model = response.Model,
            CreatedAt = response.Created.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(response.Created.Value).UtcDateTime
                : DateTime.UtcNow
        };
    }

    internal static ChatResponse ToChatResponse(this AnthropicChatResponse response)
    {
        var text = response.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;
        return new ChatResponse
        {
            Content = text,
            FinishReason = response.StopReason,
            Usage = new Usage
            {
                InputTokens = response.Usage?.InputTokens ?? 0,
                OutputTokens = response.Usage?.OutputTokens ?? 0
            },
            Model = response.Model,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static ChatResponse ToChatResponse(this GoogleChatResponse response)
    {
        var candidate = response.Candidates?.FirstOrDefault();
        var content = candidate?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        return new ChatResponse
        {
            Content = content,
            FinishReason = candidate?.FinishReason,
            Usage = new Usage
            {
                InputTokens = response.UsageMetadata?.PromptTokenCount ?? 0,
                OutputTokens = response.UsageMetadata?.CandidatesTokenCount ?? 0
            },
            Model = "gemini",
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static ChatResponse ToChatResponse(this OllamaChatResponse response)
    {
        var content = response.Message?.Content ?? string.Empty;
        return new ChatResponse
        {
            Content = content,
            FinishReason = response.DoneReason,
            Usage = new Usage
            {
                InputTokens = response.EvalCount ?? 0,
                OutputTokens = response.PromptEvalCount ?? 0
            },
            Model = response.Model,
            CreatedAt = DateTime.UtcNow
        };
    }
}