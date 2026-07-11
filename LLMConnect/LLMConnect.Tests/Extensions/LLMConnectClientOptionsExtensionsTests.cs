using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Extensions;

public class LLMConnectClientOptionsExtensionsTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public LLMConnectClientOptionsExtensionsTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
    }

    // ---------- InternalComputedDefaultModel ----------

    [Fact]
    public void InternalComputedDefaultModel_WhenModelProvided_ReturnsProvidedModel()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };
        var providedModel = "gpt-4-turbo";

        // Act
        var result = options.InternalComputedDefaultModel(providedModel);

        // Assert
        result.Should().Be("gpt-4-turbo");
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenModelNotProvided_ReturnsDefaultModelFromOptions()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be("gpt-4");
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenModelAndDefaultModelNotProvided_ReturnsProviderDefault()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be("gpt-5.5");
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, "gpt-5.5")]
    [InlineData(ProviderType.Anthropic, "claude-sonnet-5")]
    [InlineData(ProviderType.Google, "gemini-3.5-flash")]
    [InlineData(ProviderType.Ollama, "qwen2.5:7b-instruct")]
    [InlineData(ProviderType.AzureOpenAI, "gpt-4")]
    public void InternalComputedDefaultModel_ReturnsCorrectProviderDefault(ProviderType provider, string expected)
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = provider
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenProviderNotSupported_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;
        var options = new LLMConnectGeneralOptions
        {
            Provider = unsupportedProvider
        };

        // Act
        Action act = () => options.InternalComputedDefaultModel();

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }

    [Fact]
    public void InternalComputedDefaultModel_Precedence_ModelOverridesDefaultModel()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };
        var providedModel = "gpt-4-turbo";

        // Act
        var result = options.InternalComputedDefaultModel(providedModel);

        // Assert
        result.Should().Be("gpt-4-turbo");
    }

    [Fact]
    public void InternalComputedDefaultModel_Precedence_DefaultModelOverridesProviderDefault()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be("gpt-4");
    }

    [Fact]
    public void InternalComputedDefaultModel_WithEmptyStringModel_IgnoresEmptyString()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };

        // Act
        var result = options.InternalComputedDefaultModel("");

        // Assert
        result.Should().Be("gpt-4");
    }

    [Fact]
    public void InternalComputedDefaultModel_WithWhitespaceModel_IgnoresWhitespace()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = "gpt-4"
        };

        // Act
        var result = options.InternalComputedDefaultModel("   ");

        // Assert
        result.Should().Be("gpt-4");
    }

    [Fact]
    public void InternalComputedDefaultModel_ForAzureOpenAI_ReturnsAzureDefault()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.AzureOpenAI
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be("gpt-4");
    }

    // ---------- ToGeneralOptions ----------

    [Fact]
    public void ToGeneralOptions_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var legacyOptions = new LLMConnectClientOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-api-key",
            DefaultModel = "claude-3-5-sonnet",
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetries = 5,
            LoggerFactory = _loggerFactoryMock.Object
        };

        // Act
        var result = legacyOptions.ToGeneralOptions();

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be(legacyOptions.Provider);
        result.ApiKey.Should().Be(legacyOptions.ApiKey);
        result.DefaultModel.Should().Be(legacyOptions.DefaultModel);
        result.Timeout.Should().Be(legacyOptions.Timeout);
        result.MaxRetries.Should().Be(legacyOptions.MaxRetries);
        result.LoggerFactory.Should().Be(legacyOptions.LoggerFactory);
    }

    [Fact]
    public void ToGeneralOptions_WithNullInput_ReturnsEmptyGeneralOptions()
    {
        // Arrange
        LLMConnectClientOptions? legacyOptions = null;

        // Act
        var result = legacyOptions.ToGeneralOptions();

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be(ProviderType.OpenAI);
        result.ApiKey.Should().BeEmpty();
        result.DefaultModel.Should().BeNull();
        result.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        result.MaxRetries.Should().Be(3);
        result.LoggerFactory.Should().BeNull();
    }

    [Fact]
    public void ToGeneralOptions_WithPartialProperties_MapsWhatIsAvailable()
    {
        // Arrange
        var legacyOptions = new LLMConnectClientOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "google-key"
        };

        // Act
        var result = legacyOptions.ToGeneralOptions();

        // Assert
        result.Provider.Should().Be(ProviderType.Google);
        result.ApiKey.Should().Be("google-key");
        result.DefaultModel.Should().BeNull();
        result.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        result.MaxRetries.Should().Be(3);
        result.LoggerFactory.Should().BeNull();
    }

    // ---------- ToEndpointOptions ----------

    [Fact]
    public void ToEndpointOptions_MapsAllAzureAndOllamaPropertiesCorrectly()
    {
        // Arrange
        var legacyOptions = new LLMConnectClientOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview",
            OllamaPort = 12345,
            ExtraOptions = new Dictionary<string, object> { { "custom", "value" } }
        };

        // Act
        var result = legacyOptions.ToEndpointOptions();

        // Assert
        result.Should().NotBeNull();
        result.AzureResourceName.Should().Be(legacyOptions.AzureResourceName);
        result.AzureDeploymentName.Should().Be(legacyOptions.AzureDeploymentName);
        result.AzureApiVersion.Should().Be(legacyOptions.AzureApiVersion);
        result.OllamaPort.Should().Be(legacyOptions.OllamaPort);
        result.ExtraOptions.Should().BeEquivalentTo(legacyOptions.ExtraOptions);
    }

    [Fact]
    public void ToEndpointOptions_WithNullInput_ReturnsEmptyEndpointOptions()
    {
        // Arrange
        LLMConnectClientOptions? legacyOptions = null;

        // Act
        var result = legacyOptions.ToEndpointOptions();

        // Assert
        result.Should().NotBeNull();
        result.AzureResourceName.Should().BeNull();
        result.AzureDeploymentName.Should().BeNull();
        result.AzureApiVersion.Should().BeNull();
        result.OllamaPort.Should().BeNull();
        result.ExtraOptions.Should().BeNull();
    }

    [Fact]
    public void ToEndpointOptions_WithPartialAzureProperties_MapsWhatIsAvailable()
    {
        // Arrange
        var legacyOptions = new LLMConnectClientOptions
        {
            AzureResourceName = "my-resource"
        };

        // Act
        var result = legacyOptions.ToEndpointOptions();

        // Assert
        result.AzureResourceName.Should().Be("my-resource");
        result.AzureDeploymentName.Should().BeNull();
        result.AzureApiVersion.Should().BeNull();
        result.OllamaPort.Should().BeNull();
        result.ExtraOptions.Should().BeNull();
    }

    [Fact]
    public void ToEndpointOptions_WithExtraOptions_MapsExtraOptionsCorrectly()
    {
        // Arrange
        var extraOptions = new Dictionary<string, object>
        {
            { "custom-header", "value1" },
            { "custom-setting", 123 }
        };
        var legacyOptions = new LLMConnectClientOptions
        {
            ExtraOptions = extraOptions
        };

        // Act
        var result = legacyOptions.ToEndpointOptions();

        // Assert
        result.ExtraOptions.Should().NotBeNull();
        result.ExtraOptions.Should().ContainKey("custom-header");
        result.ExtraOptions["custom-header"].Should().Be("value1");
        result.ExtraOptions.Should().ContainKey("custom-setting");
        result.ExtraOptions["custom-setting"].Should().Be(123);
    }
}