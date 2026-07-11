using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Validation;

public class GoogleEmbeddingRequestValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly GoogleEmbeddingRequestValidator _validator;

    public GoogleEmbeddingRequestValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new GoogleEmbeddingRequestValidator();
    }

    // ---------- Valid TaskType ----------

    [Theory]
    [InlineData("RETRIEVAL_DOCUMENT")]
    [InlineData("RETRIEVAL_QUERY")]
    [InlineData("CLASSIFICATION")]
    [InlineData("CLUSTERING")]
    [InlineData("SEMANTIC_SIMILARITY")]
    public void Validate_WhenTaskTypeValid_DoesNotThrow(string taskType)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            TaskType = taskType
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    // ---------- Invalid TaskType ----------

    [Theory]
    [InlineData("INVALID")]
    [InlineData("UNKNOWN")]
    [InlineData(" ")] // Whitespace is not empty, so it will be validated and fail
    public void Validate_WhenTaskTypeInvalid_ThrowsArgumentException(string taskType)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            TaskType = taskType
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.TaskType))
            .WithMessage("TaskType must be one of: RETRIEVAL_DOCUMENT, RETRIEVAL_QUERY, CLASSIFICATION, CLUSTERING, SEMANTIC_SIMILARITY.*");

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TaskType must be one of")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenTaskTypeNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            TaskType = null
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    // ---------- Title Validation ----------

    [Fact]
    public void Validate_WhenTitleIsWhitespace_ThrowsArgumentException()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Title = "   "
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Title))
            .WithMessage("Title cannot be whitespace.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Title cannot be whitespace")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenTitleIsNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Title = null
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenTitleIsValid_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Title = "My Document"
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    // ---------- Role Validation ----------

    [Theory]
    [InlineData("user")]
    [InlineData("model")]
    public void Validate_WhenRoleValid_DoesNotThrow(string role)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Role = role
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("system")]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenRoleInvalid_ThrowsArgumentException(string role)
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Role = role
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Role))
            .WithMessage("Role must be 'user' or 'model'.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Role must be 'user' or 'model'")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenRoleNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Role = null
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
            TaskType = "RETRIEVAL_QUERY",
            Title = "My Query",
            Role = "user"
        };

        _validator.Validate(request, _loggerMock.Object);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google embedding request validated successfully.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Base Validation (Text) ----------

    [Fact]
    public void Validate_WhenTextIsNull_ThrowsArgumentException_FromBase()
    {
        var request = new EmbeddingRequest { Text = null! };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Text))
            .WithMessage("Embedding text cannot be null or whitespace.*");
        // No provider-specific logging should occur because base validation fails first
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Google embedding request validated successfully.")),
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
            TaskType = "RETRIEVAL_DOCUMENT"
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            TaskType = "INVALID"
        };

        Action act = () => _validator.Validate(request, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.TaskType));
    }
}