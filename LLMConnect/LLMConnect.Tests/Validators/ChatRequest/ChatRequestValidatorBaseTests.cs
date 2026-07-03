using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Request;

public class ChatRequestValidatorBaseTests
{
    internal class TestChatRequestValidator : ChatRequestValidatorBase
    {
        public bool ProviderSpecificCalled { get; private set; }

        protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
        {
            ProviderSpecificCalled = true;
            // No additional validation for testing the base class
        }
    }

    private readonly TestChatRequestValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public ChatRequestValidatorBaseTests()
    {
        _validator = new TestChatRequestValidator();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Validate_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _validator.Validate(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void Validate_WhenMessagesIsNull_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest { Messages = null! };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages))
            .WithMessage("At least one message is required.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("At least one message is required")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenMessagesIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest { Messages = new List<Message>() };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_WhenTemperatureOutOfRange_ThrowsArgumentException(float temperature)
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Temperature = temperature
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Temperature))
            .WithMessage("Temperature must be between 0 and 1.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Temperature must be between 0 and 1")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenMaxTokensLessThanOne_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            MaxTokens = 0
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.MaxTokens))
            .WithMessage("MaxTokens must be greater than 0.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MaxTokens must be greater than 0")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenStopSequencesContainsEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            StopSequences = new List<string> { " ", "" }
        };

        // Act
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.StopSequences))
            .WithMessage("StopSequences cannot contain empty or whitespace strings.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("StopSequences cannot contain empty or whitespace strings")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllValid_CallsProviderSpecificValidation()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Temperature = 0.5f,
            MaxTokens = 10,
            StopSequences = new List<string> { "stop" }
        };

        // Act
        _validator.Validate(request, _loggerMock.Object);

        // Assert
        _validator.ProviderSpecificCalled.Should().BeTrue();
        // No exceptions thrown, and logger not called for errors
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hello") },
            Temperature = 0.5f,
            MaxTokens = 10,
            StopSequences = new List<string> { "stop" }
        };

        // Act
        Action act = () => _validator.Validate(request, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        // Arrange
        var request = new ChatRequest { Messages = null! };

        // Act
        Action act = () => _validator.Validate(request, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));
    }
}