using LLMConnect.Models;

namespace LLMConnect;

internal static class ChatRequestBuilderExtensions
{
    internal static OpenAIChatRequest ToOpenAIRequest(this ChatRequest request, string? defaultModel = null)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = MessageRole.System.ToString().ToLower(), Content = request.SystemPrompt });
        }

        foreach (var msg in request.Messages)
        {
            switch(msg.Role)
            {
                case MessageRole.Assistant:
                    messages.Add(new OpenAIMessage { Role = "assistant", Content = msg.Content });
                    break;
                case MessageRole.System:
                    messages.Add(new OpenAIMessage { Role = "system", Content = msg.Content });
                    break;
                case MessageRole.User:
                    messages.Add(new OpenAIMessage { Role = "user", Content = msg.Content });
                    break;
                default:
                    messages.Add(new OpenAIMessage { Role = MapToOpenAIRole(msg.Role), Content = msg.Content });
                    break;
            }
        }

        var model = request.Model ?? defaultModel; 

        var openAiRequest = new OpenAIChatRequest
        {
            Model = model ?? string.Empty,
            Messages = messages,
            Temperature = request.Temperature != 0.0f ? request.Temperature : null,
            TopP = request.TopP != 0.0f ? request.TopP : null,
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : null,
            Stop = request.StopSequences,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            ResponseFormat = request.ResponseFormat != null ? new OpenAIResponseFormat { Type = request.ResponseFormat } : null,
            Seed = request.Seed,
            User = request.User
        };

        // Merge extra parameters
        if (request.ExtraParameters != null && request.ExtraParameters.Count > 0)
            openAiRequest.ExtraData = new Dictionary<string, object>(request.ExtraParameters); 

        return openAiRequest;
    }

    internal static AnthropicChatRequest ToAnthropicRequest(this ChatRequest request, string? defaultModel = null)
    {
        var messages = new List<AnthropicMessage>();
        foreach (var msg in request.Messages)
        {
            var role = MapToAnthropicRole(msg.Role);

            if (msg.Role != MessageRole.System)
                messages.Add(new AnthropicMessage { Role = role, Content = msg.Content });
        }

        var model = request.Model ?? defaultModel;

        return new AnthropicChatRequest
        {
            Model = model,
            Messages = messages,
            System = string.IsNullOrWhiteSpace(request.SystemPrompt) ? null : request.SystemPrompt,
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : 1024,
            Temperature = request.Temperature != 0.0f ? request.Temperature : null,
            TopP = request.TopP != 0.0f ? request.TopP : null,
            Stream = null, // Set externally
            StopSequences = request.StopSequences
        };
    }

    internal static GoogleChatRequest ToGoogleRequest(this ChatRequest request, string? defaultModel = null)
    {
        var contents = new List<GoogleContent>();
        GoogleContent? systemContent = null;

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            systemContent = new GoogleContent
            {
                Role = "system",
                Parts = new List<GooglePart> { new GooglePart { Text = request.SystemPrompt } }
            };
        }

        foreach (var msg in request.Messages)
        {
            contents.Add(new GoogleContent
            {
                Role = MapToGoogleRole(msg.Role),
                Parts = new List<GooglePart> { new GooglePart { Text = msg.Content } }
            });
        }

        var generationConfig = new GoogleGenerationConfig
        {
            Temperature = request.Temperature != 0.0f ? request.Temperature : null,
            TopP = request.TopP != 0.0f ? request.TopP : null,
            MaxOutputTokens = request.MaxTokens > 0 ? request.MaxTokens : null,
            // StopSequences not directly supported; use ExtraParameters if needed
        };

        return new GoogleChatRequest
        {
            Contents = contents,
            SystemInstruction = systemContent,
            GenerationConfig = generationConfig
        };
    }

    internal static OllamaChatRequest ToOllamaRequest(this ChatRequest request, string defaultModel)
    {
        var messages = new List<OllamaMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new OllamaMessage { Role = "system", Content = request.SystemPrompt });

        foreach (var msg in request.Messages)
            messages.Add(new OllamaMessage { Role = MapToOllamaRole(msg.Role), Content = msg.Content });

        var model = request.Model ?? defaultModel;

        return new OllamaChatRequest
        {
            Model = model,
            Messages = messages,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature != 0.0f ? request.Temperature : null,
                TopP = request.TopP != 0.0f ? request.TopP : null,
                NumPredict = request.MaxTokens > 0 ? request.MaxTokens : null,
                Stop = request.StopSequences,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Seed = request.Seed
            }
        };
    }

    internal static string MapToOpenAIRole(MessageRole role) => role switch
    {
        MessageRole.System => "system",
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        MessageRole.Tool => "tool",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    internal static string MapToAnthropicRole(MessageRole role) => role switch
    {
        MessageRole.System => "user",     // System prompts are a top-level property
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        MessageRole.Tool => "user",       // Tool messages are not natively supported
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    internal static string MapToGoogleRole(MessageRole role) => role switch
    {
        MessageRole.System => "system",
        MessageRole.User => "user",
        MessageRole.Assistant => "model",
        MessageRole.Tool => "user",       // Not natively supported
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    internal static string MapToOllamaRole(MessageRole role) => role switch
    {
        MessageRole.System => "system",
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        MessageRole.Tool => "tool",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}