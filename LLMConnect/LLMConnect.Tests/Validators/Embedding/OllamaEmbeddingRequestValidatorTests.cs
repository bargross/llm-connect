using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Validation;

public class OllamaEmbeddingRequestValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly OllamaEmbeddingRequestValidator _validator;

    public OllamaEmbeddingRequestValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new OllamaEmbeddingRequestValidator();
    }

    // ---------- ExtraParameters ----------

    [Fact]
    public void Validate_WhenExtraParametersExist_LogsInformationWithCount()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object>
            {
                ["num_ctx"] = 2048,
                ["temperature"] = 0.5
            }
        };

        _validator.Validate(request, _loggerMock.Object);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request contains extra parameters: 2")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenExtraParametersExist_AlsoLogsSuccess()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object> { ["key"] = "value" }
        };

        _validator.Validate(request, _loggerMock.Object);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenExtraParametersNull_LogsOnlySuccess()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = null
        };

        _validator.Validate(request, _loggerMock.Object);

        // Should NOT log about extra parameters
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request contains extra parameters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // Should log success
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenExtraParametersEmpty_LogsOnlySuccess()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object>()
        };

        _validator.Validate(request, _loggerMock.Object);

        // Should NOT log about extra parameters (empty dict is skipped)
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request contains extra parameters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // Should log success
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Valid Request ----------

    [Fact]
    public void Validate_WhenAllValid_LogsSuccess()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Model = "nomic-embed-text"
        };

        _validator.Validate(request, _loggerMock.Object);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Base Validation ----------

    [Fact]
    public void Validate_WhenTextIsNull_ThrowsArgumentException_FromBase()
    {
        var request = new EmbeddingRequest { Text = null! };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Text))
            .WithMessage("Embedding text cannot be null or whitespace.*");
        // Success log should NOT be called because base validation fails first
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow_ForValidRequest()
    {
        var request = new EmbeddingRequest { Text = "Hello" };

        Action act = () => _validator.Validate(request, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_AndExtraParametersExist_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object> { ["key"] = "value" }
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows_FromBase()
    {
        var request = new EmbeddingRequest { Text = null! };

        Action act = () => _validator.Validate(request, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Text));
    }
}