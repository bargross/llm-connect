using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class OllamaOptionsValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly OllamaOptionsValidator _validator;

    public OllamaOptionsValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new OllamaOptionsValidator();
    }

    // ---------- Port Validation ----------

    [Theory]
    [InlineData(1)]
    [InlineData(11434)]
    [InlineData(65535)]
    public void Validate_WhenOllamaPortValid_DoesNotThrow(int validPort)
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = validPort };

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().NotThrow();
        _loggerMock.Verify(x => x.Log(
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
    public void Validate_WhenOllamaPortLessThanOne_ThrowsArgumentException(int invalidPort)
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = invalidPort };

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectEndpointOptions.OllamaPort))
            .WithMessage($"Invalid port: {invalidPort}. Must be between 1 and 65535.*");
        _loggerMock.Verify(x => x.Log(
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
    public void Validate_WhenOllamaPortGreaterThanMax_ThrowsArgumentException(int invalidPort)
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = invalidPort };

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectEndpointOptions.OllamaPort))
            .WithMessage($"Invalid port: {invalidPort}. Must be between 1 and 65535.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid port: {invalidPort}. Must be between 1 and 65535.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenOllamaPortNull_DoesNotThrow()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = null };

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().NotThrow();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Base Validation (API Key Not Required) ----------

    [Fact]
    public void Validate_WhenApiKeyMissingForOllama_DoesNotThrow()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama, ApiKey = null };
        var endpoint = new LLMConnectEndpointOptions();

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().NotThrow();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Missing api key")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Other General Validation (Timeout, MaxRetries, etc.) ----------

    [Fact]
    public void Validate_WhenTimeoutZero_ThrowsArgumentException()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            Timeout = TimeSpan.Zero,
            MaxRetries = 3
        };
        var endpoint = new LLMConnectEndpointOptions();

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectGeneralOptions.Timeout))
            .WithMessage("Timeout must be greater than zero.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Timeout must be greater than zero")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenMaxRetriesNegative_ThrowsArgumentException()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null,
            MaxRetries = -1
        };
        var endpoint = new LLMConnectEndpointOptions();

        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectGeneralOptions.MaxRetries))
            .WithMessage("MaxRetries must be >= 0.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MaxRetries must be >= 0")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama, ApiKey = null };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = 11434 };

        Action act = () => _validator.Validate(general, endpoint, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.Ollama };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = 0 };

        Action act = () => _validator.Validate(general, endpoint, null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectEndpointOptions.OllamaPort));
    }
}