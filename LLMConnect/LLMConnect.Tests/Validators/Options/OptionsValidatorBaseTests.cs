using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class OptionsValidationBaseTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestOptionsValidator _validator;

    public OptionsValidationBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new TestOptionsValidator();
    }

    // ---------- Test Subclass ----------
    private class TestOptionsValidator : OptionsValidationBase
    {
        public bool GeneralOptionsValidated { get; private set; }
        public bool EndpointOptionsValidated { get; private set; }

        protected override void ValidateProviderSpecificGeneralOptions(LLMConnectGeneralOptions generalOptions, ILogger? logger)
        {
            GeneralOptionsValidated = true;
        }

        protected override void ValidateProviderSpecificEndpointOptions(LLMConnectEndpointOptions endpointOptions, ILogger? logger)
        {
            EndpointOptionsValidated = true;
        }
    }

    // ---------- Null Checks ----------

    [Fact]
    public void Validate_WhenGeneralOptionsNull_ThrowsArgumentNullException()
    {
        Action act = () => _validator.Validate(null!, new LLMConnectEndpointOptions(), _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("generalOptions");
    }

    [Fact]
    public void Validate_WhenEndpointOptionsNull_ThrowsArgumentNullException()
    {
        Action act = () => _validator.Validate(new LLMConnectGeneralOptions(), null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("endpointOptions");
    }

    // ---------- API Key Validation ----------

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    public void Validate_WhenApiKeyMissingForCloudProvider_ThrowsArgumentException(ProviderType provider)
    {
        var general = new LLMConnectGeneralOptions { Provider = provider, ApiKey = null };
        var endpoint = new LLMConnectEndpointOptions();
        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Missing api key for provider {provider}*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Missing api key for provider {provider}")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

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
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Timeout Validation ----------

    [Fact]
    public void Validate_WhenTimeoutZero_ThrowsArgumentException()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key", Timeout = TimeSpan.Zero };
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

    // ---------- MaxRetries Validation ----------

    [Fact]
    public void Validate_WhenMaxRetriesNegative_ThrowsArgumentException()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key", MaxRetries = -1 };
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

    // ---------- DefaultModel Length ----------

    [Fact]
    public void Validate_WhenDefaultModelExceedsMaxLength_ThrowsArgumentException()
    {
        var longModel = new string('a', 101);
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key", DefaultModel = longModel };
        var endpoint = new LLMConnectEndpointOptions();
        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectGeneralOptions.DefaultModel))
            .WithMessage("DefaultModel cannot exceed 100 characters.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DefaultModel cannot exceed 100 characters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Endpoint Validation ----------

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://")]
    public void Validate_WhenEndpointInvalid_ThrowsArgumentException(string invalidEndpoint)
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key" };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = invalidEndpoint };
        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectEndpointOptions.Endpoint))
            .WithMessage($"Invalid endpoint URL: {invalidEndpoint} for provider OpenAI*");
        _loggerMock.Verify(x => x.Log(
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
    public void Validate_WhenEndpointNotHttps_ThrowsArgumentException(ProviderType provider)
    {
        var general = new LLMConnectGeneralOptions { Provider = provider, ApiKey = "key" };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = "http://api.example.com" };
        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(LLMConnectEndpointOptions.Endpoint))
            .WithMessage($"Endpoint must use HTTPS for provider '{provider}'.*");
        _loggerMock.Verify(x => x.Log(
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
    public void Validate_WhenEndpointLocalhost_AllowsHttp(string localhostEndpoint)
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key" };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = localhostEndpoint };
        Action act = () => _validator.Validate(general, endpoint, _loggerMock.Object);
        act.Should().NotThrow<ArgumentException>();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Endpoint must use HTTPS")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Validate_WhenEndpointValidAndHttps_DoesNotThrow()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key" };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = "https://api.openai.com/v1" };
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

    // ---------- OpenAI Non-Standard Endpoint Warning ----------

    [Fact]
    public void Validate_WhenOpenAIEndpointNonStandard_LogsWarning()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = "key" };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = "https://custom-proxy.com/v1" };
        _validator.Validate(general, endpoint, _loggerMock.Object);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI provider used with non-OpenAI endpoint")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Provider-Specific Methods Called ----------

    [Fact]
    public void Validate_WhenAllValid_CallsProviderSpecificMethods()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            DefaultModel = "gpt-4"
        };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = "https://api.openai.com/v1" };
        _validator.Validate(general, endpoint, _loggerMock.Object);
        _validator.GeneralOptionsValidated.Should().BeTrue();
        _validator.EndpointOptionsValidated.Should().BeTrue();
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        var endpoint = new LLMConnectEndpointOptions { Endpoint = "https://api.openai.com/v1" };
        Action act = () => _validator.Validate(general, endpoint, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        var general = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI, ApiKey = null };
        var endpoint = new LLMConnectEndpointOptions();
        Action act = () => _validator.Validate(general, endpoint, null);
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Missing api key for provider OpenAI*");
    }
}