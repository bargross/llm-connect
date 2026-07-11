using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validation;

public class AnthropicEmbeddingRequestValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly AnthropicEmbeddingRequestValidator _validator;

    public AnthropicEmbeddingRequestValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new AnthropicEmbeddingRequestValidator();
    }

    // ---------- Throws NotSupportedException ----------

    [Fact]
    public void Validate_WhenCalled_ThrowsNotSupportedException()
    {
        var request = new EmbeddingRequest { Text = "Hello" };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Anthropic does not support embedding generation.");
    }

    [Fact]
    public void Validate_WhenCalled_LogsError()
    {
        var request = new EmbeddingRequest { Text = "Hello" };

        try
        {
            _validator.Validate(request, _loggerMock.Object);
        }
        catch (NotSupportedException) { }

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anthropic does not support embedding generation.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_StillThrows()
    {
        var request = new EmbeddingRequest { Text = "Hello" };

        Action act = () => _validator.Validate(request, null);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Anthropic does not support embedding generation.");
        // No log verification because logger is null
    }

    [Fact]
    public void Validate_WithNullRequest_ThrowsNotSupportedException()
    {
        // Note: The validator does not check for null request; it directly throws.
        // This is expected behavior for Anthropic validator.

        Action act = () => _validator.Validate(null!, _loggerMock.Object);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Anthropic does not support embedding generation.");
        // It still logs the error even if request is null.
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anthropic does not support embedding generation.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WithEmptyRequest_ThrowsNotSupportedException()
    {
        var request = new EmbeddingRequest { Text = "" };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Anthropic does not support embedding generation.");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anthropic does not support embedding generation.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}