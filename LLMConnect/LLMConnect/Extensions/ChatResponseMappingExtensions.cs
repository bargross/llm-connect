using LLMConnect.Models;
using System.Text.Json;

namespace LLMConnect;

internal static class ChatResponseMappingExtensions
{
    internal static ChatResponse? ToChatResponse(this OpenAIChatResponse response)
    {
        if (response == null) return null;

        var firstChoice = response.Choices?.FirstOrDefault();
        var message = firstChoice?.Message;

        var chatResponse = new ChatResponse
        {
            Content = message?.Content ?? string.Empty,
            FinishReason = firstChoice?.FinishReason,
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

        if (message?.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            chatResponse.ToolCalls = message.ToolCalls.Select(tc => new ToolCall
            {
                Id = tc.Id,
                Name = tc.Function.Name,
                Arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(tc.Function.Arguments ?? "{}") ?? new()
            }).ToList();
        }

        return chatResponse;
    }

    internal static ChatResponse? ToChatResponse(this AnthropicChatResponse response)
    {
        if (response == null) return null;

        var textBlock = response.Content?.FirstOrDefault(c => c.Type == "text");
        var toolUseBlocks = response.Content?.Where(c => c.Type == "tool_use").ToList();

        var chatResponse = new ChatResponse
        {
            Content = textBlock?.Text ?? string.Empty,
            FinishReason = response.StopReason,
            Usage = new Usage
            {
                InputTokens = response.Usage?.InputTokens ?? 0,
                OutputTokens = response.Usage?.OutputTokens ?? 0
            },
            Model = response.Model,
            CreatedAt = DateTime.UtcNow
        };

        if (toolUseBlocks != null && toolUseBlocks.Count > 0)
        {
            chatResponse.ToolCalls = toolUseBlocks.Select(c => new ToolCall
            {
                Id = c.Id ?? string.Empty,
                Name = c.Name ?? string.Empty,
                Arguments = c.Input ?? new()
            }).ToList();
        }

        return chatResponse;
    }

    internal static ChatResponse? ToChatResponse(this GoogleChatResponse response)
    {
        if (response == null) return null;

        var candidate = response.Candidates?.FirstOrDefault();
        var content = candidate?.Content;
        var parts = content?.Parts;

        var textPart = parts?.FirstOrDefault(p => p.Text != null);
        var functionPart = parts?.FirstOrDefault(p => p.FunctionCall != null);

        var googleResponse = new ChatResponse
        {
            Content = textPart?.Text ?? string.Empty,
            FinishReason = candidate?.FinishReason,
            Usage = new Usage
            {
                InputTokens = response.UsageMetadata?.PromptTokenCount ?? 0,
                OutputTokens = response.UsageMetadata?.CandidatesTokenCount ?? 0
            },
            Model = "gemini",
            CreatedAt = DateTime.UtcNow
        };

        if (functionPart?.FunctionCall != null)
        {
            googleResponse.ToolCalls = new List<ToolCall>
        {
            new ToolCall
            {
                Id = functionPart.FunctionCall.Name ?? string.Empty,
                Name = functionPart.FunctionCall.Name ?? string.Empty,
                Arguments = functionPart.FunctionCall.Args ?? new()
            }
        };
        }

        return googleResponse;
    }

    internal static ChatResponse? ToChatResponse(this OllamaChatResponse response)
    {
        if (response == null) return null;

        var content = response.Message?.Content ?? string.Empty;
        var ollamaResponse = new ChatResponse
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

        if (response.Message?.ToolCalls != null && response.Message.ToolCalls.Count > 0)
        {
            ollamaResponse.ToolCalls = response.Message.ToolCalls.Select(tc => new ToolCall
            {
                Id = tc.Function?.Name ?? string.Empty,
                Name = tc.Function?.Name ?? string.Empty,
                Arguments = tc.Function?.Arguments ?? new()
            }).ToList();
        }


        return ollamaResponse;
    }
}