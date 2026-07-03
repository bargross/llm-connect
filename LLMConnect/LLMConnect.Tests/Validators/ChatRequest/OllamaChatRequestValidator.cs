using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Request;

public class OllamaChatRequestValidatorTests
{
    private readonly OllamaChatRequestValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public OllamaChatRequestValidatorTests()
    {
        _validator = new OllamaChatRequestValidator();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Validate_WhenResponseFormatIsProvided_LogsWarning()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "json_object"
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support ResponseFormat")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenSeedIsProvided_LogsWarning()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Seed = 42
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support Seed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenFrequencyPenaltyIsProvided_LogsWarning()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            FrequencyPenalty = 0.5f
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support FrequencyPenalty")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenPresencePenaltyIsProvided_LogsWarning()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            PresencePenalty = 0.5f
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support PresencePenalty")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllUnsupportedFieldsAreProvided_LogsAllWarnings()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "json_object",
            Seed = 42,
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.5f
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(4));
    }

    [Fact]
    public void Validate_WhenRequestIsValid_LogsInformation()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Temperature = 0.5f,
            MaxTokens = 10
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama chat request validated successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "json_object",
            Seed = 42,
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.5f
        };

        // Act
        Action act = () => _validator.Validate(request, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenBaseValidationFails_ThrowsArgumentException_BeforeProviderSpecificValidation()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message>(), // Empty messages — base validation fails
            ResponseFormat = "json_object",
            Seed = 42,
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.5f
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));

        // Provider-specific warnings should NOT be logged because base validation threw
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama does not support")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}