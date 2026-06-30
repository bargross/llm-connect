using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using Xunit;

namespace LLMConnect.Tests;

public class ProviderBaseTests
{
    private class TestProvider : ProviderBase { }

    private readonly TestProvider _provider;
    private readonly Mock<ILogger> _loggerMock;

    public ProviderBaseTests()
    {
        _provider = new TestProvider();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public async Task ExtractErrorMessage_WithOpenAIErrorFormat_ReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithAnthropicErrorFormat_ReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid request"}}""", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be("Invalid request");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithGoogleErrorFormat_ReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"API key not valid"}}""", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be("API key not valid");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithTopLevelMessage_ReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Service unavailable"}""", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be("Service unavailable");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithNonJsonBody_ReturnsRawBodyWithStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be($"HTTP error: {HttpStatusCode.InternalServerError} - Internal Server Error");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithEmptyBody_ReturnsStatusCodeOnly()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        result.Should().Be($"HTTP error: {HttpStatusCode.NotFound} - ");
    }

    [Fact]
    public async Task ExtractErrorMessage_WithMalformedJson_ReturnsStatusCodeAndRawBody()
    {
        // Arrange
        var content = "{invalid json";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        // Assert
        // The method should return: "HTTP error: BadRequest - {invalid json"
        // because it falls back to the raw body when JSON parsing fails.
        result.Should().Be($"HTTP error: {HttpStatusCode.BadRequest} - {content}");
    }

    [Fact]
    public async Task LogAndThrow_LogsErrorAndThrowsLLMConnectException()
    {
        // Arrange
        var providerType = ProviderType.OpenAI;
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        // Act
        Func<Task> act = async () => await _provider.LogAndThrow(providerType, response, _loggerMock.Object, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Invalid API key");

        // The log message is the provider name, not the exception message.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAndThrow_WhenProviderIsOllama_ProviderNameIsTitleCase()
    {
        // Arrange
        var providerType = ProviderType.Ollama;
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Error"}""", Encoding.UTF8, "application/json")
        };

        // Act
        Func<Task> act = async () => await _provider.LogAndThrow(providerType, response, _loggerMock.Object, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
    }

    [Fact]
    public async Task LogAndThrow_WhenLoggerIsNull_StillThrows()
    {
        // Arrange
        var providerType = ProviderType.OpenAI;
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        // Act
        Func<Task> act = async () => await _provider.LogAndThrow(providerType, response, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<LLMConnectException>()
            .WithMessage("Invalid API key");
        // No logging verification because logger is null
    }
}