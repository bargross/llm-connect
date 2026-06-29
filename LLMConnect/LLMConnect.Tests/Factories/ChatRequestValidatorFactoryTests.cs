using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests;

public class ChatRequestValidatorFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;

    public ChatRequestValidatorFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIChatRequestValidator))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicChatRequestValidator))]
    [InlineData(ProviderType.Google, typeof(GoogleChatRequestValidator))]
    [InlineData(ProviderType.Ollama, typeof(OllamaChatRequestValidator))]
    public void Create_ForSupportedProviders_ReturnsCorrectValidatorType(ProviderType provider, Type expectedType)
    {
        // Act
        var validator = ChatRequestValidatorFactory.Create(provider, _loggerMock.Object);

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
        Action act = () => ChatRequestValidatorFactory.Create(unsupportedProvider, _loggerMock.Object);

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
        Action act = () => ChatRequestValidatorFactory.Create(ProviderType.OpenAI, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WhenUnsupportedProviderAndLoggerIsNull_StillThrows()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => ChatRequestValidatorFactory.Create(unsupportedProvider, null);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");
        // No log verification because logger is null
    }

    [Fact]
    public void Create_ForOpenAI_ReturnsOpenAIChatRequestValidator()
    {
        // Act
        var validator = ChatRequestValidatorFactory.Create(ProviderType.OpenAI, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<OpenAIChatRequestValidator>();
        validator.Should().BeAssignableTo<IChatRequestValidator>();
    }

    [Fact]
    public void Create_ForAnthropic_ReturnsAnthropicChatRequestValidator()
    {
        // Act
        var validator = ChatRequestValidatorFactory.Create(ProviderType.Anthropic, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<AnthropicChatRequestValidator>();
        validator.Should().BeAssignableTo<IChatRequestValidator>();
    }

    [Fact]
    public void Create_ForGoogle_ReturnsGoogleChatRequestValidator()
    {
        // Act
        var validator = ChatRequestValidatorFactory.Create(ProviderType.Google, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<GoogleChatRequestValidator>();
        validator.Should().BeAssignableTo<IChatRequestValidator>();
    }

    [Fact]
    public void Create_ForOllama_ReturnsOllamaChatRequestValidator()
    {
        // Act
        var validator = ChatRequestValidatorFactory.Create(ProviderType.Ollama, _loggerMock.Object);

        // Assert
        validator.Should().BeOfType<OllamaChatRequestValidator>();
        validator.Should().BeAssignableTo<IChatRequestValidator>();
    }
}