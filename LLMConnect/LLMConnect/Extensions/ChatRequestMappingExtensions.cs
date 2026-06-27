using LLMConnect.Models;
using System.Text.Json;

namespace LLMConnect;

internal static class ChatRequestBuilderExtensions
{
    // ---------- OpenAI ----------
    internal static OpenAIChatRequest ToOpenAIRequest(this ChatRequest request, string? defaultModel = null)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });
        }

        foreach (var msg in request.Messages)
        {
            if (msg is SystemMessage systemMsg)
                messages.Add(new OpenAIMessage { Role = "system", Content = systemMsg.Content });
            else if (msg is UserMessage userMsg)
                messages.Add(new OpenAIMessage { Role = "user", Content = userMsg.Content });
            else if (msg is AssistantMessage assistantMsg)
                messages.Add(new OpenAIMessage { Role = "assistant", Content = assistantMsg.Content });
            else
                messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
        }

        var model = request.Model ?? defaultModel ?? "gpt-3.5-turbo";

        var openAiRequest = new OpenAIChatRequest
        {
            Model = model,
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
        {
            openAiRequest.ExtraData = new Dictionary<string, object>(request.ExtraParameters);
        }

        return openAiRequest;
    }

    // ---------- Anthropic ----------
    internal static AnthropicChatRequest ToAnthropicRequest(this ChatRequest request, string? defaultModel = null)
    {
        var messages = new List<AnthropicMessage>();
        foreach (var msg in request.Messages)
        {
            string role = msg.Role switch
            {
                "system" => "user",     // System prompts are handled separately
                "assistant" => "assistant",
                _ => "user"
            };
            if (msg.Role != "system")
                messages.Add(new AnthropicMessage { Role = role, Content = msg.Content });
        }

        var model = request.Model ?? defaultModel ?? "claude-3-5-sonnet-20241022";

        return new AnthropicChatRequest
        {
            Model = model,
            Messages = messages,
            System = string.IsNullOrEmpty(request.SystemPrompt) ? null : request.SystemPrompt,
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : 1024,
            Temperature = request.Temperature != 0.0f ? request.Temperature : null,
            TopP = request.TopP != 0.0f ? request.TopP : null,
            Stream = null, // Set externally
            StopSequences = request.StopSequences
        };
    }

    // ---------- Google ----------
    internal static GoogleChatRequest ToGoogleRequest(this ChatRequest request, string? defaultModel = null)
    {
        var contents = new List<GoogleContent>();
        GoogleContent? systemContent = null;

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            systemContent = new GoogleContent
            {
                Role = "system",
                Parts = new List<GooglePart> { new GooglePart { Text = request.SystemPrompt } }
            };
        }

        foreach (var msg in request.Messages)
        {
            var role = msg.Role switch
            {
                "user" => "user",
                "assistant" => "model",
                "system" => "system",
                _ => "user"
            };
            contents.Add(new GoogleContent
            {
                Role = role,
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

    // ---------- Ollama ----------
    internal static OllamaChatRequest ToOllamaRequest(this ChatRequest request, string? defaultModel = null)
    {
        var messages = new List<OllamaMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new OllamaMessage { Role = "system", Content = request.SystemPrompt });
        foreach (var msg in request.Messages)
            messages.Add(new OllamaMessage { Role = msg.Role, Content = msg.Content });

        var model = request.Model ?? defaultModel ?? "llama3.2";

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
}