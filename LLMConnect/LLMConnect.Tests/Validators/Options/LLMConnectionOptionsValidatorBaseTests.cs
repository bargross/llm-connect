using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Validators.Options;

public class LLMConnectOptionsValidationBaseTests
{
    internal class TestOptionsValidator : LLMConnectOptionsValidationBase
    {
        public bool ProviderSpecificCalled { get; private set; }

        protected override void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger)
        {
            ProviderSpecificCalled = true;
            logger?.LogInformation("Provider-specific validation called.");
        }
    }

    private readonly TestOptionsValidator _validator;
    private readonly Mock<ILogger> _loggerMock;

    public LLMConnectOptionsValidationBaseTests()
    {
        _validator = new TestOptionsValidator();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Validate_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _validator.Validate(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    public void Validate_WhenApiKeyIsMissing_ThrowsArgumentException(ProviderType provider)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = null!,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Missing api key for provider {provider}*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Missing api key for provider {provider}")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenProviderIsOllama_ApiKeyIsNotRequired()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null!,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow<ArgumentException>();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Missing api key")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenTimeoutIsZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.Zero,
            MaxRetries = 3
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.Timeout))
            .WithMessage("Timeout must be greater than zero.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Timeout must be greater than zero")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenMaxRetriesIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = -1
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.MaxRetries))
            .WithMessage("MaxRetries must be >= 0.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MaxRetries must be >= 0")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenDefaultModelExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            DefaultModel = new string('a', 101)
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.DefaultModel))
            .WithMessage("DefaultModel cannot exceed 100 characters.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DefaultModel cannot exceed 100 characters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://")]
    public void Validate_WhenEndpointIsInvalid_ThrowsArgumentException(string invalidEndpoint)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = invalidEndpoint
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.Endpoint))
            .WithMessage($"Invalid endpoint URL: {invalidEndpoint} for provider OpenAI*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid endpoint URL: {invalidEndpoint}")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    public void Validate_WhenEndpointIsNotHttps_ThrowsArgumentException(ProviderType provider)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = "http://api.example.com"
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectClientOptions.Endpoint))
            .WithMessage($"Endpoint must use HTTPS for provider '{provider}'.*");

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Endpoint must use HTTPS for provider '{provider}'")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://127.0.0.1")]
    public void Validate_WhenEndpointIsLocalhost_AllowsHttp(string localhostEndpoint)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = localhostEndpoint
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow<ArgumentException>();
        // No error log about HTTPS
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Endpoint must use HTTPS")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenEndpointIsValidAndHttps_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-api-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = "https://api.openai.com/v1"
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
        // No error logs
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenOpenAIEndpointIsNonStandard_LogsWarning()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = "https://custom-proxy.com/v1"
        };

        // Act
        _validator.Validate(options, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI provider used with non-OpenAI endpoint")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllOptionsAreValid_CallsProviderSpecificValidation()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            DefaultModel = "gpt-3.5-turbo",
            Endpoint = "https://api.openai.com/v1"
        };

        // Act
        _validator.Validate(options, _loggerMock.Object);

        // Assert
        _validator.ProviderSpecificCalled.Should().BeTrue();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Provider-specific validation called")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            Endpoint = "https://api.openai.com/v1"
        };

        // Act
        Action act = () => _validator.Validate(options, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenProviderSpecificValidationThrows_PropagatesException()
    {
        // Arrange
        var throwingValidator = new ThrowingTestOptionsValidator();
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };

        // Act
        Action act = () => throwingValidator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Provider-specific validation failed.");
    }

    // Helper test class that throws in provider-specific validation
    private class ThrowingTestOptionsValidator : LLMConnectOptionsValidationBase
    {
        protected override void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger)
        {
            throw new InvalidOperationException("Provider-specific validation failed.");
        }
    }
}