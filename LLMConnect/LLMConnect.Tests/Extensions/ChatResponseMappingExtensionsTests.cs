using FluentAssertions;

namespace LLMConnect.Tests;

public class ChatResponseMappingExtensionsTests
{
    // ---------- OpenAI ----------

    [Fact]
    public void ToChatResponse_OpenAI_WithValidResponse_ReturnsChatResponse()
    {
        // Arrange
        var response = new OpenAIChatResponse
        {
            Id = "chatcmpl-123",
            Model = "gpt-3.5-turbo",
            Created = 1677651234,
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice
                {
                    Index = 0,
                    Message = new OpenAIResponseMessage { Content = "Hello, world!" },
                    FinishReason = "stop"
                }
            },
            Usage = new OpenAIUsage
            {
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("stop");
        result.Model.Should().Be("gpt-3.5-turbo");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
        result.CreatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1677651234).UtcDateTime);
    }

    [Fact]
    public void ToChatResponse_OpenAI_WithNullResponse_ReturnsNull()
    {
        // Arrange
        OpenAIChatResponse? response = null;

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToChatResponse_OpenAI_WithNoChoices_ReturnsEmptyContent()
    {
        // Arrange
        var response = new OpenAIChatResponse
        {
            Model = "gpt-3.5-turbo",
            Choices = new List<OpenAIChoice>(),
            Usage = new OpenAIUsage { PromptTokens = 0, CompletionTokens = 0 }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.FinishReason.Should().BeNull();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_OpenAI_WithNullMessage_ReturnsEmptyContent()
    {
        // Arrange
        var response = new OpenAIChatResponse
        {
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice { Message = null }
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void ToChatResponse_OpenAI_WithNullUsage_ReturnsZeroUsage()
    {
        // Arrange
        var response = new OpenAIChatResponse
        {
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice { Message = new OpenAIResponseMessage { Content = "Hello" } }
            },
            Usage = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_OpenAI_WithNoCreated_ReturnsCurrentTime()
    {
        // Arrange
        var response = new OpenAIChatResponse
        {
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice { Message = new OpenAIResponseMessage { Content = "Hello" } }
            },
            Created = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    // ---------- Anthropic ----------

    [Fact]
    public void ToChatResponse_Anthropic_WithValidResponse_ReturnsChatResponse()
    {
        // Arrange
        var response = new AnthropicChatResponse
        {
            Id = "msg_123",
            Model = "claude-3-5-sonnet-20241022",
            StopReason = "end_turn",
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Hello, world!" }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 10,
                OutputTokens = 5
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("end_turn");
        result.Model.Should().Be("claude-3-5-sonnet-20241022");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToChatResponse_Anthropic_WithNullResponse_ReturnsNull()
    {
        // Arrange
        AnthropicChatResponse? response = null;

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToChatResponse_Anthropic_WithNoContent_ReturnsEmptyContent()
    {
        // Arrange
        var response = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>(),
            Usage = new AnthropicUsage { InputTokens = 0, OutputTokens = 0 }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_Anthropic_WithNullContent_ReturnsEmptyContent()
    {
        // Arrange
        var response = new AnthropicChatResponse
        {
            Content = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void ToChatResponse_Anthropic_WithNullUsage_ReturnsZeroUsage()
    {
        // Arrange
        var response = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Hello" }
            },
            Usage = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_Anthropic_WithNoTextBlock_ReturnsEmptyContent()
    {
        // Arrange
        var response = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "other", Text = null }
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    // ---------- Google ----------

    [Fact]
    public void ToChatResponse_Google_WithValidResponse_ReturnsChatResponse()
    {
        // Arrange
        var response = new GoogleChatResponse
        {
            Candidates = new List<GoogleCandidate>
            {
                new GoogleCandidate
                {
                    Content = new GoogleContent
                    {
                        Parts = new List<GooglePart> { new GooglePart { Text = "Hello, world!" } }
                    },
                    FinishReason = "STOP"
                }
            },
            UsageMetadata = new GoogleUsageMetadata
            {
                PromptTokenCount = 10,
                CandidatesTokenCount = 5
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("STOP");
        result.Model.Should().Be("gemini");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToChatResponse_Google_WithNullResponse_ReturnsNull()
    {
        // Arrange
        GoogleChatResponse? response = null;

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToChatResponse_Google_WithNoCandidates_ReturnsEmptyContent()
    {
        // Arrange
        var response = new GoogleChatResponse
        {
            Candidates = new List<GoogleCandidate>(),
            UsageMetadata = new GoogleUsageMetadata { PromptTokenCount = 0, CandidatesTokenCount = 0 }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_Google_WithNullCandidates_ReturnsEmptyContent()
    {
        // Arrange
        var response = new GoogleChatResponse
        {
            Candidates = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void ToChatResponse_Google_WithNoParts_ReturnsEmptyContent()
    {
        // Arrange
        var response = new GoogleChatResponse
        {
            Candidates = new List<GoogleCandidate>
            {
                new GoogleCandidate { Content = new GoogleContent { Parts = new List<GooglePart>() } }
            }
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void ToChatResponse_Google_WithNullUsage_ReturnsZeroUsage()
    {
        // Arrange
        var response = new GoogleChatResponse
        {
            Candidates = new List<GoogleCandidate>
            {
                new GoogleCandidate
                {
                    Content = new GoogleContent
                    {
                        Parts = new List<GooglePart> { new GooglePart { Text = "Hello" } }
                    }
                }
            },
            UsageMetadata = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    // ---------- Ollama ----------

    [Fact]
    public void ToChatResponse_Ollama_WithValidResponse_ReturnsChatResponse()
    {
        // Arrange
        var response = new OllamaChatResponse
        {
            Model = "llama3.2",
            Message = new OllamaMessage { Role = "assistant", Content = "Hello, world!" },
            Done = true,
            DoneReason = "stop",
            EvalCount = 10,
            PromptEvalCount = 5
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("stop");
        result.Model.Should().Be("llama3.2");
        result.Usage.InputTokens.Should().Be(10); // EvalCount maps to InputTokens
        result.Usage.OutputTokens.Should().Be(5); // PromptEvalCount maps to OutputTokens
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToChatResponse_Ollama_WithNullResponse_ReturnsNull()
    {
        // Arrange
        OllamaChatResponse? response = null;

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToChatResponse_Ollama_WithNoMessage_ReturnsEmptyContent()
    {
        // Arrange
        var response = new OllamaChatResponse
        {
            Model = "llama3.2",
            Message = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void ToChatResponse_Ollama_WithNullEvalCount_ReturnsZeroUsage()
    {
        // Arrange
        var response = new OllamaChatResponse
        {
            Message = new OllamaMessage { Content = "Hello" },
            EvalCount = null,
            PromptEvalCount = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(0);
        result.Usage.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void ToChatResponse_Ollama_WithNullDoneReason_FinisReasonIsNull()
    {
        // Arrange
        var response = new OllamaChatResponse
        {
            Message = new OllamaMessage { Content = "Hello" },
            DoneReason = null
        };

        // Act
        var result = response.ToChatResponse();

        // Assert
        result.Should().NotBeNull();
        result.FinishReason.Should().BeNull();
    }
}