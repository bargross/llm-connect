using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Validators.Options;

public class OptionsValidatorFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;

    public OptionsValidatorFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIOptionsValidator))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicOptionsValidator))]
    [InlineData(ProviderType.Google, typeof(GoogleOptionsValidator))]
    [InlineData(ProviderType.Ollama, typeof(OllamaOptionsValidator))]
    public void Create_ForSupportedProviders_ReturnsCorrectValidatorType(ProviderType provider, Type expectedType)
    {
        // Act
        var validator = OptionsValidatorFactory.Create(provider, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType(expectedType);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Create_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => OptionsValidatorFactory.Create(unsupportedProvider, _loggerMock.Object);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Provider '{unsupportedProvider.ToString()}' is not supported.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Create_WhenLoggerIsNull_DoesNotThrow()
    {
        // Act
        Action act = () => OptionsValidatorFactory.Create(ProviderType.OpenAI, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WhenUnsupportedProviderAndLoggerIsNull_StillThrows()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => OptionsValidatorFactory.Create(unsupportedProvider, null);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");
        // No log verification because logger is null
    }

    [Fact]
    public void Create_ForOpenAI_ReturnsCorrectValidator()
    {
        // Act
        var validator = OptionsValidatorFactory.Create(ProviderType.OpenAI, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<OpenAIOptionsValidator>();
        // Ensure the validator is not null and is of the correct base type
        validator.Should().BeAssignableTo<IOptionsValidator>();
    }

    [Fact]
    public void Create_ForAnthropic_ReturnsCorrectValidator()
    {
        // Act
        var validator = OptionsValidatorFactory.Create(ProviderType.Anthropic, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<AnthropicOptionsValidator>();
        validator.Should().BeAssignableTo<IOptionsValidator>();
    }

    [Fact]
    public void Create_ForGoogle_ReturnsCorrectValidator()
    {
        // Act
        var validator = OptionsValidatorFactory.Create(ProviderType.Google, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<GoogleOptionsValidator>();
        validator.Should().BeAssignableTo<IOptionsValidator>();
    }

    [Fact]
    public void Create_ForOllama_ReturnsCorrectValidator()
    {
        // Act
        var validator = OptionsValidatorFactory.Create(ProviderType.Ollama, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<OllamaOptionsValidator>();
        validator.Should().BeAssignableTo<IOptionsValidator>();
    }
}