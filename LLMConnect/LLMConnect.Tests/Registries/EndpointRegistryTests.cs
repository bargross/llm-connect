using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace LLMConnect.Registries;

public class EndpointRegistryTests
{

    public static TheoryData<ProviderType, int, bool, string> EndpointParamsData =>
        new()
        {
            // OpenAI
            { ProviderType.OpenAI, (int)QueryType.Chat, false, "chat/completions" },
            { ProviderType.OpenAI, (int)QueryType.Chat, true, "chat/completions" },
            { ProviderType.OpenAI, (int)QueryType.Embeddings, false, "embeddings" },

            // Anthropic
            { ProviderType.Anthropic, (int)QueryType.Chat, false, "messages" },
            { ProviderType.Anthropic, (int)QueryType.Chat, true, "messages" },

            // Google
            { ProviderType.Google, (int)QueryType.Chat, false, "models/{model}:generateContent" },
            { ProviderType.Google, (int)QueryType.Chat, true, "models/{model}:streamGenerateContent" },
            { ProviderType.Google, (int)QueryType.Embeddings, false, "models/{model}:embedContent" },

            // Ollama
            { ProviderType.Ollama, (int)QueryType.Chat, false, "api/chat" },
            { ProviderType.Ollama, (int)QueryType.Chat, true, "api/chat" },
            { ProviderType.Ollama, (int)QueryType.Embeddings, false, "api/embed" },
        };


    // ---- GetDefaultEndpoint tests ----

    [Theory]
    [InlineData(ProviderType.OpenAI, 11434, "https://api.openai.com/v1/")]
    [InlineData(ProviderType.Anthropic, 11434, "https://api.anthropic.com/v1/")]
    [InlineData(ProviderType.Google, 11434, "https://generativelanguage.googleapis.com/v1beta/")]
    [InlineData(ProviderType.Ollama, 11434, "http://localhost:11434/")]
    [InlineData(ProviderType.Ollama, 8080, "http://localhost:8080/")]
    public void GetDefaultEndpoint_ValidProvider_ReturnsExpectedString(ProviderType provider, int port, string expected)
    {
        // Act
        var result = EndpointRegistry.GetDefaultEndpoint(provider, port);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDefaultEndpoint_NullProvider_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetDefaultEndpoint_UnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(unsupportedProvider);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetDefaultEndpoint_WithLogger_LogsErrorOnUnsupportedProvider()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => EndpointRegistry.GetDefaultEndpoint(unsupportedProvider, logger: mockLogger.Object);

        // Assert
        act.Should().Throw<NotSupportedException>();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("is not supported")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    // ---- GetEndpointParams tests ----

    [Theory]
    [MemberData(nameof(EndpointParamsData))]
    public void GetEndpointParams_ValidCombination_ReturnsExpectedPath(
        ProviderType provider,
        int queryType,
        bool isStreaming,
        string expected)
    {
        // Act
        var result = EndpointRegistry.GetEndpointParams(provider, (QueryType)queryType, isStreaming);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetEndpointParams_UnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => EndpointRegistry.GetEndpointParams(unsupportedProvider, QueryType.Chat, false);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetEndpointParams_UnsupportedCombination_ThrowsNotSupportedException()
    {
        // Anthropic does not support Embeddings, and this combination is not in the dictionary.
        // Act
        Action act = () => EndpointRegistry.GetEndpointParams(ProviderType.Anthropic, QueryType.Embeddings, false);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetEndpointParams_WithLogger_LogsErrorOnUnsupportedCombination()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act
        Action act = () => EndpointRegistry.GetEndpointParams(
            ProviderType.Anthropic,
            QueryType.Embeddings,
            false,
            logger: mockLogger.Object);

        // Assert
        act.Should().Throw<NotSupportedException>();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not supported")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    // ---- Combined usage tests (optional) ----

    [Theory]
    [InlineData(ProviderType.Google, (int)QueryType.Chat, false, "gemini-3.5-flash",
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent")]
    [InlineData(ProviderType.Google, (int)QueryType.Chat, true, "gemini-3.5-flash",
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:streamGenerateContent")]
    [InlineData(ProviderType.Ollama, (int)QueryType.Embeddings, false, null,
                "http://localhost:11434/api/embed")]
    public void GetFullUrl_CombiningBaseAndPath_ReturnsExpectedFullUrl(
        ProviderType provider,
        int queryType,
        bool isStreaming,
        string model,
        string expectedFullUrl)
    {
        // Arrange
        var baseUrl = EndpointRegistry.GetDefaultEndpoint(provider);
        var relativePath = EndpointRegistry.GetEndpointParams(provider, (QueryType)queryType, isStreaming);

        // Replace model placeholder if present
        if (model is not null)
            relativePath = relativePath.Replace("{model}", model);

        var fullUrl = baseUrl + relativePath;

        // Assert
        fullUrl.Should().Be(expectedFullUrl);
    }

    // ---- Edge cases for placeholders ----

    [Fact]
    public void GetEndpointParams_ReturnsPlaceholder_ForGoogleModels()
    {
        // Arrange
        var provider = ProviderType.Google;
        var queryType = QueryType.Chat;

        // Act
        var path = EndpointRegistry.GetEndpointParams(provider, queryType, false);

        // Assert
        path.Should().Contain("{model}");
    }

    [Fact]
    public void GetDefaultEndpoint_ReplacesPortPlaceholder_ForOllama()
    {
        // Arrange
        var provider = ProviderType.Ollama;
        var customPort = 12345;

        // Act
        var baseUrl = EndpointRegistry.GetDefaultEndpoint(provider, customPort);

        // Assert
        baseUrl.Should().Be($"http://localhost:{customPort}/");
    }
}