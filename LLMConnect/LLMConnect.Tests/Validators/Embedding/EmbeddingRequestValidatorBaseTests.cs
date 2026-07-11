using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validation;

public class EmbeddingRequestValidatorBaseTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestEmbeddingRequestValidator _validator;

    public EmbeddingRequestValidatorBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new TestEmbeddingRequestValidator();
    }

    // ---------- Test Subclass ----------
    private class TestEmbeddingRequestValidator : EmbeddingRequestValidatorBase
    {
        public bool ProviderSpecificCalled { get; private set; }

        protected override void ValidateProviderSpecific(EmbeddingRequest request, ILogger? logger)
        {
            ProviderSpecificCalled = true;
            // No additional validation for testing
        }
    }

    // ---------- Request Null ----------

    [Fact]
    public void Validate_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _validator.Validate(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    // ---------- Text Validation ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_WhenTextIsNullOrWhitespace_ThrowsArgumentException(string? text)
    {
        var request = new EmbeddingRequest { Text = text };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Text))
            .WithMessage("Embedding text cannot be null or whitespace.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Embedding text cannot be null or whitespace")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Model Length ----------

    [Fact]
    public void Validate_WhenModelExceedsMaxLength_ThrowsArgumentException()
    {
        var longModel = new string('a', 101);
        var request = new EmbeddingRequest { Text = "Hello", Model = longModel };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Model))
            .WithMessage("Model name cannot exceed 100 characters.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Model name exceeds 100 characters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenModelIsNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest { Text = "Hello", Model = null };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().NotThrow();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenModelIsEmpty_DoesNotThrow()
    {
        var request = new EmbeddingRequest { Text = "Hello", Model = "" };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().NotThrow();
        // Empty string is not validated (only null or whitespace in check, but Model is not checked for whitespace)
        // So it passes.
    }

    // ---------- Dimensions ----------

    [Fact]
    public void Validate_WhenDimensionsLessThanOne_ThrowsArgumentException()
    {
        var request = new EmbeddingRequest { Text = "Hello", Dimensions = 0 };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Dimensions))
            .WithMessage("Dimensions must be a positive integer.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dimensions must be a positive integer")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenDimensionsNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest { Text = "Hello", Dimensions = null };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenDimensionsPositive_DoesNotThrow()
    {
        var request = new EmbeddingRequest { Text = "Hello", Dimensions = 128 };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().NotThrow();
    }

    // ---------- Valid Request ----------

    [Fact]
    public void Validate_WhenAllValid_CallsProviderSpecificValidation()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            Model = "text-embedding-3-small",
            Dimensions = 256
        };
        _validator.Validate(request, _loggerMock.Object);
        _validator.ProviderSpecificCalled.Should().BeTrue();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        var request = new EmbeddingRequest { Text = "Hello" };
        Action act = () => _validator.Validate(request, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        var request = new EmbeddingRequest { Text = null };
        Action act = () => _validator.Validate(request, null);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(EmbeddingRequest.Text));
    }
}