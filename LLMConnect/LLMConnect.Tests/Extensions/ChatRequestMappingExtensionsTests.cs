using FluentAssertions;
using LLMConnect.Models;

namespace LLMConnect.Tests.MappingExtensions;

public class ChatRequestBuilderExtensionsTests
{
    private readonly ChatRequest _baseRequest;

    public ChatRequestBuilderExtensionsTests()
    {
        _baseRequest = new ChatRequest
        {
            Messages = new List<Message>
            {
                new UserMessage("Hello"),
                new AssistantMessage("Hi there"),
                new SystemMessage("You are a helpful assistant.")
            },
            SystemPrompt = "System prompt override",
            Temperature = 0.8f,
            TopP = 0.95f,
            MaxTokens = 150,
            Model = "custom-model",
            StopSequences = new List<string> { "stop" },
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.6f,
            ResponseFormat = "json_object",
            Seed = 42,
            User = "test-user",
            ExtraParameters = new Dictionary<string, object> { { "extra", "value" } }
        };
    }

    // ---------- OpenAI ----------

    [Fact]
    public void ToOpenAIRequest_WithCompleteRequest_ReturnsCorrectDto()
    {
        // Arrange
        var request = _baseRequest;

        // Act
        var result = request.ToOpenAIRequest("default-model");

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().Be("custom-model");
        result.Messages.Should().HaveCount(3);

        // Order: System prompt from SystemPrompt property comes first
        result.Messages[0].Role.Should().Be("system");
        result.Messages[0].Content.Should().Be("System prompt override");

        // Then the messages from the Messages list (excluding system messages)
        result.Messages[1].Role.Should().Be("user");
        result.Messages[1].Content.Should().Be("Hello");
        result.Messages[2].Role.Should().Be("assistant");
        result.Messages[2].Content.Should().Be("Hi there");

        result.Temperature.Should().Be(0.8f);
        result.TopP.Should().Be(0.95f);
        result.MaxTokens.Should().Be(150);
        result.Stop.Should().BeEquivalentTo(new List<string> { "stop" });
        result.FrequencyPenalty.Should().Be(0.5f);
        result.PresencePenalty.Should().Be(0.6f);
        result.ResponseFormat.Should().NotBeNull();
        result.ResponseFormat.Type.Should().Be("json_object");
        result.Seed.Should().Be(42);
        result.User.Should().Be("test-user");
        result.ExtraData.Should().ContainKey("extra").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void ToOpenAIRequest_WithDefaultModel_UsesDefault()
    {
        // Arrange
        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act
        var result = request.ToOpenAIRequest("default-model");

        // Assert
        result.Model.Should().Be("default-model");
    }

    [Fact]
    public void ToOpenAIRequest_WithTemperatureZero_DoesNotSetTemperature()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Temperature = 0.0f
        };

        // Act
        var result = request.ToOpenAIRequest();

        // Assert
        result.Temperature.Should().BeNull();
    }

    [Fact]
    public void ToOpenAIRequest_WithMaxTokensZero_DoesNotSetMaxTokens()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            MaxTokens = 0
        };

        // Act
        var result = request.ToOpenAIRequest();

        // Assert
        result.MaxTokens.Should().BeNull();
    }

    [Fact]
    public void ToOpenAIRequest_WithExtraParameters_CopiesExtraData()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            ExtraParameters = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act
        var result = request.ToOpenAIRequest();

        // Assert
        result.ExtraData.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void ToOpenAIRequest_WithNoExtraParameters_ExtraDataIsNull()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        var result = request.ToOpenAIRequest();

        // Assert
        result.ExtraData.Should().BeNull();
    }

    [Fact]
    public void ToOpenAIRequest_WithToolMessage_UsesToolRole()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>
            {
                new ToolMessage("call_123", "tool response")
            }
        };

        // Act
        var result = request.ToOpenAIRequest();

        // Assert
        result.Messages[0].Role.Should().Be("tool");
        result.Messages[0].Content.Should().Be("tool response");
    }

    // ---------- Anthropic ----------

    [Fact]
    public void ToAnthropicRequest_WithCompleteRequest_ReturnsCorrectDto()
    {
        // Arrange
        var request = _baseRequest;

        // Act
        var result = request.ToAnthropicRequest("default-model");

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().Be("custom-model");
        result.Messages.Should().HaveCount(2); // System message excluded
        result.Messages[0].Role.Should().Be("user");
        result.Messages[0].Content.Should().Be("Hello");
        result.Messages[1].Role.Should().Be("assistant");
        result.Messages[1].Content.Should().Be("Hi there");
        result.System.Should().Be("System prompt override"); // Top-level system
        result.MaxTokens.Should().Be(150);
        result.Temperature.Should().Be(0.8f);
        result.TopP.Should().Be(0.95f);
        result.StopSequences.Should().BeEquivalentTo(new List<string> { "stop" });
        result.Stream.Should().BeNull();
    }

    [Fact]
    public void ToAnthropicRequest_WithDefaultModel_UsesDefault()
    {
        // Arrange
        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act
        var result = request.ToAnthropicRequest("default-model");

        // Assert
        result.Model.Should().Be("default-model");
    }

    [Fact]
    public void ToAnthropicRequest_WithSystemMessage_ExcludedFromMessages()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>
            {
                new SystemMessage("System message"),
                new UserMessage("User message")
            }
        };

        // Act
        var result = request.ToAnthropicRequest();

        // Assert
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Role.Should().Be("user");
        result.Messages[0].Content.Should().Be("User message");
    }

    [Fact]
    public void ToAnthropicRequest_WithNoSystemPrompt_SystemIsNull()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            SystemPrompt = null
        };

        // Act
        var result = request.ToAnthropicRequest();

        // Assert
        result.System.Should().BeNull();
    }

    [Fact]
    public void ToAnthropicRequest_WithMaxTokensZero_UsesDefault1024()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            MaxTokens = 0
        };

        // Act
        var result = request.ToAnthropicRequest();

        // Assert
        result.MaxTokens.Should().Be(1024);
    }

    [Fact]
    public void ToAnthropicRequest_WithToolMessage_MapsToolToUser()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>
            {
                new ToolMessage("call_123", "tool response")
            }
        };

        // Act
        var result = request.ToAnthropicRequest();

        // Assert
        result.Messages[0].Role.Should().Be("user");
    }

    // ---------- Google ----------

    [Fact]
    public void ToGoogleRequest_WithCompleteRequest_ReturnsCorrectDto()
    {
        // Arrange
        var request = _baseRequest;

        // Act
        var result = request.ToGoogleRequest("default-model");

        // Assert
        result.Should().NotBeNull();
        result.Contents.Should().HaveCount(3);
        result.Contents[0].Role.Should().Be("user");
        result.Contents[0].Parts[0].Text.Should().Be("Hello");
        result.Contents[1].Role.Should().Be("model");
        result.Contents[1].Parts[0].Text.Should().Be("Hi there");
        result.Contents[2].Role.Should().Be("system");
        result.Contents[2].Parts[0].Text.Should().Be("You are a helpful assistant.");
        result.SystemInstruction.Should().NotBeNull();
        result.SystemInstruction.Role.Should().Be("system");
        result.SystemInstruction.Parts[0].Text.Should().Be("System prompt override");
        result.GenerationConfig.Temperature.Should().Be(0.8f);
        result.GenerationConfig.TopP.Should().Be(0.95f);
        result.GenerationConfig.MaxOutputTokens.Should().Be(150);
    }

    [Fact]
    public void ToGoogleRequest_WithDefaultModel_UsesDefault()
    {
        // Arrange - Google does not use model in request, so this test is less relevant.
        // We just ensure it doesn't throw.
        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act
        var result = request.ToGoogleRequest("default-model");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ToGoogleRequest_WithNoSystemPrompt_SystemInstructionIsNull()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            SystemPrompt = null
        };

        // Act
        var result = request.ToGoogleRequest();

        // Assert
        result.SystemInstruction.Should().BeNull();
    }

    [Fact]
    public void ToGoogleRequest_WithToolMessage_MapsToolToUser()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>
            {
                new ToolMessage("call_123", "tool response")
            }
        };

        // Act
        var result = request.ToGoogleRequest();

        // Assert
        result.Contents[0].Role.Should().Be("user");
    }

    // ---------- Ollama ----------

    [Fact]
    public void ToOllamaRequest_WithCompleteRequest_ReturnsCorrectDto()
    {
        // Arrange
        var request = _baseRequest;

        // Act
        var result = request.ToOllamaRequest("default-model");

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().Be("custom-model");
        result.Messages.Should().HaveCount(4);
        result.Messages[0].Role.Should().Be("system");
        result.Messages[0].Content.Should().Be("System prompt override");
        result.Messages[1].Role.Should().Be("user");
        result.Messages[1].Content.Should().Be("Hello");
        result.Messages[2].Role.Should().Be("assistant");
        result.Messages[2].Content.Should().Be("Hi there");
        result.Messages[3].Role.Should().Be("system");
        result.Messages[3].Content.Should().Be("You are a helpful assistant.");
        result.Options.Temperature.Should().Be(0.8f);
        result.Options.TopP.Should().Be(0.95f);
        result.Options.NumPredict.Should().Be(150);
        result.Options.Stop.Should().BeEquivalentTo(new List<string> { "stop" });
        result.Options.FrequencyPenalty.Should().Be(0.5f);
        result.Options.PresencePenalty.Should().Be(0.6f);
        result.Options.Seed.Should().Be(42);
    }

    [Fact]
    public void ToOllamaRequest_WithDefaultModel_UsesDefault()
    {
        // Arrange
        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act
        var result = request.ToOllamaRequest("default-model");

        // Assert
        result.Model.Should().Be("default-model");
    }

    [Fact]
    public void ToOllamaRequest_WithNoSystemPrompt_NoSystemMessageAdded()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            SystemPrompt = null
        };

        // Act
        var result = request.ToOllamaRequest("default-model");

        // Assert
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Role.Should().Be("user");
    }

    [Fact]
    public void ToOllamaRequest_WithToolMessage_MapsToolToTool()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>
            {
                new ToolMessage("call_123", "tool response")
            }
        };

        // Act
        var result = request.ToOllamaRequest("default-model");

        // Assert
        result.Messages[0].Role.Should().Be("tool");
    }

    // ---------- Role Mapping Methods ----------

    [Theory]
    [InlineData(MessageRole.System, "system")]
    [InlineData(MessageRole.User, "user")]
    [InlineData(MessageRole.Assistant, "assistant")]
    [InlineData(MessageRole.Tool, "tool")]
    public void MapToOpenAIRole_ReturnsCorrectString(MessageRole role, string expected)
    {
        // Act
        var result = ChatRequestMappingExtensions.MapToOpenAIRole(role);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(MessageRole.System, "user")]
    [InlineData(MessageRole.User, "user")]
    [InlineData(MessageRole.Assistant, "assistant")]
    [InlineData(MessageRole.Tool, "user")]
    public void MapToAnthropicRole_ReturnsCorrectString(MessageRole role, string expected)
    {
        // Act
        var result = ChatRequestMappingExtensions.MapToAnthropicRole(role);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(MessageRole.System, "system")]
    [InlineData(MessageRole.User, "user")]
    [InlineData(MessageRole.Assistant, "model")]
    [InlineData(MessageRole.Tool, "user")]
    public void MapToGoogleRole_ReturnsCorrectString(MessageRole role, string expected)
    {
        // Act
        var result = ChatRequestMappingExtensions.MapToGoogleRole(role);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(MessageRole.System, "system")]
    [InlineData(MessageRole.User, "user")]
    [InlineData(MessageRole.Assistant, "assistant")]
    [InlineData(MessageRole.Tool, "tool")]
    public void MapToOllamaRole_ReturnsCorrectString(MessageRole role, string expected)
    {
        // Act
        var result = ChatRequestMappingExtensions.MapToOllamaRole(role);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void MapToOpenAIRole_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => ChatRequestMappingExtensions.MapToOpenAIRole((MessageRole)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");
    }

    [Fact]
    public void MapToAnthropicRole_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => ChatRequestMappingExtensions.MapToAnthropicRole((MessageRole)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");
    }

    [Fact]
    public void MapToGoogleRole_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => ChatRequestMappingExtensions.MapToGoogleRole((MessageRole)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");
    }

    [Fact]
    public void MapToOllamaRole_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => ChatRequestMappingExtensions.MapToOllamaRole((MessageRole)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");
    }
}