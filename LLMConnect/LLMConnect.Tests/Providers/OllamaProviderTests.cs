using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace LLMConnect.Tests.Providers;

public class OllamaProviderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public OllamaProviderTests()
    {
        _loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null, // Ollama does not require an API key
            DefaultModel = "llama3.2",
            LoggerFactory = loggerFactoryMock.Object,
            MaxRetries = 0 // Disable retries for test determinism
        };
    }

    private HttpClient CreateHttpClientWithResponse(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            })
            .Verifiable();

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434/api/chat")
        };
        return client;
    }

    private HttpClient CreateHttpClientWithStreamingResponse(string ndjsonContent)
    {
        var streamContent = new StringContent(ndjsonContent, Encoding.UTF8, "application/x-ndjson");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = streamContent
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();
        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434/api/chat")
        };
        return client;
    }

    [Fact]
    public async Task ChatAsync_WhenValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseJson = @"
        {
            ""model"": ""llama3.2"",
            ""message"": {
                ""role"": ""assistant"",
                ""content"": ""Hello, world!""
            },
            ""done"": true,
            ""done_reason"": ""stop"",
            ""eval_count"": 10,
            ""prompt_eval_count"": 5
        }";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, responseJson);
        var provider = new OllamaProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Temperature = 0.7f,
            MaxTokens = 100
        };

        // Act
        var result = await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(5);
        result.Usage.OutputTokens.Should().Be(10);
    }

    [Fact]
    public async Task ChatAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var errorJson = @"{""error"":""Internal server error""}";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError, errorJson);
        var provider = new OllamaProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task ChatAsync_WhenDeserializationFails_ThrowsLLMConnectException()
    {
        // Arrange
        var invalidJson = "{ invalid: }"; // Malformed JSON
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, invalidJson);
        var provider = new OllamaProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Failed to deserialize response");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_WhenValidRequest_YieldsChatChunks()
    {
        // Arrange
        var ndjsonContent = """
        {"message":{"content":"Hello"},"done":false}
        {"message":{"content":" world"},"done":false}
        {"message":{"content":"!"},"done":true}
        """;

        var httpClient = CreateHttpClientWithStreamingResponse(ndjsonContent);
        var provider = new OllamaProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        // Act
        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Hello");
        chunks[0].IsComplete.Should().BeFalse();
        chunks[1].Content.Should().Be(" world");
        chunks[1].IsComplete.Should().BeFalse();
        chunks[2].Content.Should().Be("!");
        chunks[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task StreamAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
        var provider = new OllamaProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () =>
        {
            var enumerator = provider.StreamAsync(request, CancellationToken.None).GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        };

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal Server Error");
    }
}