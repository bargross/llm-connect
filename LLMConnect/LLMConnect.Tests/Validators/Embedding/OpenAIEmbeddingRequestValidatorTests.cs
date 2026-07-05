using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validation;

public class OpenAIEmbeddingRequestValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly OpenAIEmbeddingRequestValidator _validator;

    public OpenAIEmbeddingRequestValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new OpenAIEmbeddingRequestValidator();
    }

    // ---------- EncodingFormat Validation ----------

    [Theory]
    [InlineData("float")]
    [InlineData("base64")]
    public void Validate_WhenEncodingFormatValid_DoesNotThrow(string encodingFormat)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = encodingFormat
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("binary")]
    [InlineData("JSON")]
    public void Validate_WhenEncodingFormatInvalid_ThrowsArgumentException(string encodingFormat)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = encodingFormat
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.EncodingFormat))
            .WithMessage("EncodingFormat must be 'float' or 'base64'.*");

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EncodingFormat must be 'float' or 'base64'")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenEncodingFormatNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = null
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    // ---------- User Validation ----------

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("  ")]
    public void Validate_WhenUserIsWhitespace_ThrowsArgumentException(string user)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            User = user
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.User))
            .WithMessage("User identifier cannot be whitespace.*");

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User identifier cannot be whitespace")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenUserIsNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            User = null
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenUserIsValid_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            User = "test-user"
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    // ---------- Valid Request ----------

    [Fact]
    public void Validate_WhenAllValid_LogsInformation()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = "float",
            User = "test-user"
        };

        _validator.Validate(request, _loggerMock.Object);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI embedding request validated successfully.")),
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

        // Provider-specific logs should NOT be called
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow_ForValidRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = "float"
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_AndInvalidEncodingFormat_StillThrows()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = "invalid"
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.EncodingFormat));
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_AndInvalidUser_StillThrows()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            User = " "
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.User));
    }
}