using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class GeneralOptionsValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly GeneralOptionsValidator _validator;

    public GeneralOptionsValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new GeneralOptionsValidator();
    }

    // ---------- Provider validation ----------

    [Fact]
    public void Validate_WhenProviderIsNull_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = null
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.Provider))
            .And.Message.Should().Contain("Provider must be specified");
    }

    // ---------- API Key validation ----------

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    [InlineData(ProviderType.AzureOpenAI)]
    public void Validate_WhenApiKeyMissingForRequiredProvider_ThrowsArgumentException(ProviderType provider)
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = null // Missing
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Missing api key for provider {provider.ToString()}");
    }

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    [InlineData(ProviderType.AzureOpenAI)]
    public void Validate_WhenApiKeyIsWhitespaceForRequiredProvider_ThrowsArgumentException(ProviderType provider)
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = "   " // Whitespace
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Missing api key for provider {provider.ToString()}");
    }

    [Fact]
    public void Validate_WhenApiKeyMissingForOllama_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null // Not required
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenApiKeyProvidedForRequiredProvider_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "valid-key"
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- Timeout validation ----------

    [Fact]
    public void Validate_WhenTimeoutIsZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.Zero
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.Timeout))
            .And.Message.Should().Contain("Timeout must be greater than zero");
    }

    [Fact]
    public void Validate_WhenTimeoutIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.Timeout))
            .And.Message.Should().Contain("Timeout must be greater than zero");
    }

    [Fact]
    public void Validate_WhenTimeoutIsPositive_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- MaxRetries validation ----------

    [Fact]
    public void Validate_WhenMaxRetriesIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            MaxRetries = -1
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.MaxRetries))
            .And.Message.Should().Contain("MaxRetries must be >= 0");
    }

    [Fact]
    public void Validate_WhenMaxRetriesIsZero_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            MaxRetries = 0
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenMaxRetriesIsPositive_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            MaxRetries = 5
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- DefaultModel validation ----------

    [Fact]
    public void Validate_WhenDefaultModelExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longModel = new string('a', 101);
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            DefaultModel = longModel
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.DefaultModel))
            .And.Message.Should().Contain("DefaultModel cannot exceed 100 characters");
    }

    [Fact]
    public void Validate_WhenDefaultModelIsExactlyMaxLength_PassesValidation()
    {
        // Arrange
        var model = new string('a', 100);
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            DefaultModel = model
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenDefaultModelIsNull_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            DefaultModel = null
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenDefaultModelIsEmptyString_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            DefaultModel = ""
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenDefaultModelIsWhitespace_PassesValidation()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            DefaultModel = "   "
        };

        // Act
        Action act = () => _validator.Validate(options, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- Logging verification ----------

    [Fact]
    public void Validate_WhenProviderNull_LogsError()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions { Provider = null };

        // Act
        try { _validator.Validate(options, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Provider must be specified")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenApiKeyMissing_LogsError()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = null
        };

        // Act
        try { _validator.Validate(options, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Missing api key for provider OpenAI")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllValid_DoesNotLogError()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            DefaultModel = "gpt-4"
        };

        // Act
        _validator.Validate(options, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Null options ----------

    [Fact]
    public void Validate_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        LLMConnectGeneralOptions? options = null;

        // Act
        Action act = () => _validator.Validate(options!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("generalOptions");
    }

    // ---------- ValidateApiKey overridability (optional) ----------

    [Fact]
    public void ValidateApiKey_CanBeOverridden_InDerivedClass()
    {
        // This is more of a design test to ensure the method is protected virtual.
        // We can create a derived class and override it to verify.

        // Arrange
        var derivedValidator = new DerivedGeneralOptionsValidator();
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = null // Would normally fail
        };

        // Act
        Action act = () => derivedValidator.Validate(options, _loggerMock.Object);

        // Assert – the override should skip the API key check
        act.Should().NotThrow();
    }

    private class DerivedGeneralOptionsValidator : GeneralOptionsValidator
    {
        protected override void ValidateApiKey(LLMConnectGeneralOptions generalOptions, ILogger? logger = null)
        {
            // Override to skip API key validation
            // Do nothing
        }
    }
}