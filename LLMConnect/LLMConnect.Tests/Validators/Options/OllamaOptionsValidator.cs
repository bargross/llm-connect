using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class OllamaOptionsValidatorTests
{
    private readonly OllamaOptionsValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public OllamaOptionsValidatorTests()
    {
        _validator = new OllamaOptionsValidator();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Validate_WhenOllamaPortIsNull_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = null
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11434)]
    [InlineData(65535)]
    public void Validate_WhenOllamaPortIsValid_DoesNotThrow(int validPort)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = validPort
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenOllamaPortIsLessThanOne_ThrowsArgumentException(int invalidPort)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = invalidPort
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.OllamaPort))
            .WithMessage($"Invalid port: {invalidPort}. Must be between 1 and 65535.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid port: {invalidPort}. Must be between 1 and 65535.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(65536)]
    [InlineData(100000)]
    public void Validate_WhenOllamaPortIsGreaterThanMax_ThrowsArgumentException(int invalidPort)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = invalidPort
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.OllamaPort))
            .WithMessage($"Invalid port: {invalidPort}. Must be between 1 and 65535.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid port: {invalidPort}. Must be between 1 and 65535.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllOptionsAreValid_DoesNotLogErrors()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = 11434,
            Endpoint = "http://localhost:11434"
        };

        // Act
        _validator.Validate(options, _loggerMock.Object);

        // Assert
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
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            OllamaPort = 11434
        };

        // Act
        Action act = () => _validator.Validate(options, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenBaseValidationFails_ThrowsBeforeProviderSpecificValidation()
    {
        // Arrange: invalid timeout (base validation fails first)
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.Zero, // Invalid
            MaxRetries = 3,
            OllamaPort = 11434
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.Timeout));

        // Provider-specific error should NOT be logged because base validation fails first
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid port")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}