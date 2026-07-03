using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Validators.Request;

public class GoogleChatRequestValidatorTests
{
    private readonly GoogleChatRequestValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public GoogleChatRequestValidatorTests()
    {
        _validator = new GoogleChatRequestValidator();
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
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google does not support ResponseFormat")),
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
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google does not support Seed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenBothResponseFormatAndSeedAreProvided_LogsBothWarnings()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "json_object",
            Seed = 42
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google does not support ResponseFormat")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google does not support Seed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google chat request validated successfully")),
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
            Seed = 42
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
            ResponseFormat = "json_object", // Would log a warning, but base fails first
            Seed = 42
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
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google does not support")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}