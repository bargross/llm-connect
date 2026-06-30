using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Registries;

public class EndpointRegistryTests
{
    private readonly Mock<ILogger> _loggerMock;

    public EndpointRegistryTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, "https://api.openai.com/v1/chat/completions")]
    [InlineData(ProviderType.Anthropic, "https://api.anthropic.com/v1/messages")]
    [InlineData(ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")]
    [InlineData(ProviderType.Ollama, "http://localhost:{port}/api/chat")]
    public void GetDefaultEndpoint_ForSupportedProvider_ReturnsCorrectEndpoint(ProviderType provider, string expectedEndpoint)
    {
        // Act
        var endpoint = EndpointRegistry.GetDefaultEndpoint(provider, _loggerMock.Object);

        // Assert
        endpoint.Should().Be(expectedEndpoint);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetDefaultEndpoint_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(unsupportedProvider, _loggerMock.Object);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }

    [Fact]
    public void GetDefaultEndpoint_ForUnsupportedProvider_LogsError()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        try
        {
            EndpointRegistry.GetDefaultEndpoint(unsupportedProvider, _loggerMock.Object);
        }
        catch (NotSupportedException)
        {
            // Expected
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Provider '{unsupportedProvider}' is not supported.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetDefaultEndpoint_WhenLoggerIsNull_DoesNotThrow()
    {
        // Arrange
        var provider = ProviderType.OpenAI;

        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(provider, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetDefaultEndpoint_WhenUnsupportedProviderAndLoggerIsNull_StillThrows()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(unsupportedProvider, null);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }
}