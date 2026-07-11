using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class EndpointOptionsValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly EndpointOptionsValidator _validator;

    public EndpointOptionsValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new EndpointOptionsValidator();
    }

    // ---------- Null options ----------

    [Fact]
    public void Validate_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        LLMConnectEndpointOptions? options = null;

        // Act
        Action act = () => _validator.Validate(options!, ProviderType.OpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("endpointOptions");
    }

    // ---------- Standard providers (no endpoint options required) ----------

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    public void Validate_ForStandardProviders_DoesNotThrow_EvenWithEmptyOptions(ProviderType provider)
    {
        // Arrange
        var options = new LLMConnectEndpointOptions();

        // Act
        Action act = () => _validator.Validate(options, provider, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- Azure OpenAI validation ----------

    [Fact]
    public void Validate_ForAzureOpenAI_MissingResourceName_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = null,
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureResourceName))
            .And.Message.Should().Contain("AzureResourceName is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_ResourceNameIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "   ",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureResourceName))
            .And.Message.Should().Contain("AzureResourceName is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_MissingDeploymentName_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = null,
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureDeploymentName))
            .And.Message.Should().Contain("AzureDeploymentName is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_DeploymentNameIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "   ",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureDeploymentName))
            .And.Message.Should().Contain("AzureDeploymentName is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_MissingApiVersion_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = null
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureApiVersion))
            .And.Message.Should().Contain("AzureApiVersion is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_ApiVersionIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "   "
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.AzureApiVersion))
            .And.Message.Should().Contain("AzureApiVersion is required when using AzureOpenAI");
    }

    [Fact]
    public void Validate_ForAzureOpenAI_AllPropertiesProvided_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    // ---------- Ollama validation ----------

    [Fact]
    public void Validate_ForOllama_NoPortProvided_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = null
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ForOllama_ValidPortProvided_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 12345
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ForOllama_ValidPortAtMinimum_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 1
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ForOllama_ValidPortAtMaximum_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 65535
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ForOllama_PortBelowRange_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 0
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.OllamaPort))
            .And.Message.Should().Contain("OllamaPort must be between 1 and 65535");
    }

    [Fact]
    public void Validate_ForOllama_PortNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = -1
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.OllamaPort))
            .And.Message.Should().Contain("OllamaPort must be between 1 and 65535");
    }

    [Fact]
    public void Validate_ForOllama_PortAboveRange_ThrowsArgumentException()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 99999
        };

        // Act
        Action act = () => _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(options.OllamaPort))
            .And.Message.Should().Contain("OllamaPort must be between 1 and 65535");
    }

    // ---------- Unsupported provider ----------

    [Fact]
    public void Validate_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;
        var options = new LLMConnectEndpointOptions();

        // Act
        Action act = () => _validator.Validate(options, unsupportedProvider, _loggerMock.Object);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }

    // ---------- Logging verification ----------

    [Fact]
    public void Validate_ForAzureOpenAI_MissingResourceName_LogsError()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = null,
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        try { _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AzureResourceName is required when using AzureOpenAI")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_ForAzureOpenAI_MissingDeploymentName_LogsError()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = null,
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        try { _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AzureDeploymentName is required when using AzureOpenAI")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_ForAzureOpenAI_MissingApiVersion_LogsError()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = null
        };

        // Act
        try { _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AzureApiVersion is required when using AzureOpenAI")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_ForOllama_InvalidPort_LogsError()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            OllamaPort = 99999
        };

        // Act
        try { _validator.Validate(options, ProviderType.Ollama, _loggerMock.Object); } catch { }

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OllamaPort must be between 1 and 65535")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenAllValid_DoesNotLogError()
    {
        // Arrange
        var options = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        _validator.Validate(options, ProviderType.AzureOpenAI, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}