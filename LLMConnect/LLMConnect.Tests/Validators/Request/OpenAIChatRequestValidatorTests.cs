using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Request;

public class OpenAIChatRequestValidatorTests
{
    private readonly OpenAIChatRequestValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public OpenAIChatRequestValidatorTests()
    {
        _validator = new OpenAIChatRequestValidator();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Validate_WhenResponseFormatIsText_DoesNotThrow()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "text"
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI chat request validated successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenResponseFormatIsJsonObject_DoesNotThrow()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "json_object"
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI chat request validated successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("json")]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenResponseFormatIsInvalid_ThrowsArgumentException(string invalidFormat)
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = invalidFormat
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.ResponseFormat))
            .WithMessage("ResponseFormat must be 'text' or 'json_object'.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ResponseFormat must be 'text' or 'json_object'")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(100)]
    public void Validate_WhenSeedIsNonNegative_DoesNotThrow(int seed)
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Seed = seed
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI chat request validated successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-42)]
    [InlineData(-100)]
    public void Validate_WhenSeedIsNegative_ThrowsArgumentException(int seed)
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Seed = seed
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Seed))
            .WithMessage("Seed must be a non-negative integer.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Seed must be a non-negative integer")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenBothResponseFormatAndSeedAreInvalid_ThrowsFirstError()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            ResponseFormat = "invalid",
            Seed = -1
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.ResponseFormat))
            .WithMessage("ResponseFormat must be 'text' or 'json_object'.*");

        // Seed error should NOT be logged because ResponseFormat validation fails first
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Seed must be a non-negative integer")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenBothResponseFormatAndSeedAreValid_LogsInformation()
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
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI chat request validated successfully")),
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
            ResponseFormat = "text",
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
            ResponseFormat = "json_object", // Would be valid, but base fails first
            Seed = 42
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));

        // OpenAI-specific validation should NOT be reached because base validation threw
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ResponseFormat")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Seed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}