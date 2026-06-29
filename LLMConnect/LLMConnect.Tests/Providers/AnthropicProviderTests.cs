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

public class AnthropicProviderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public AnthropicProviderTests()
    {
        _loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-anthropic-key",
            DefaultModel = "claude-3-5-sonnet-20241022",
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
            BaseAddress = new Uri("https://api.anthropic.com/v1/messages")
        };
        return client;
    }

    private HttpClient CreateHttpClientWithStreamingResponse(string sseContent)
    {
        var streamContent = new StringContent(sseContent, Encoding.UTF8, "text/event-stream");
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
            BaseAddress = new Uri("https://api.anthropic.com/v1/messages")
        };
        return client;
    }

    [Fact]
    public async Task ChatAsync_WhenValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseJson = @"
        {
            ""id"": ""msg_123"",
            ""model"": ""claude-3-5-sonnet-20241022"",
            ""stop_reason"": ""end_turn"",
            ""content"": [
                { ""type"": ""text"", ""text"": ""Hello, world!"" }
            ],
            ""usage"": {
                ""input_tokens"": 10,
                ""output_tokens"": 5
            }
        }";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, responseJson);
        var provider = new AnthropicProvider(httpClient, _options);

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
        result.FinishReason.Should().Be("end_turn");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.Unauthorized, errorJson);
        var provider = new AnthropicProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_WhenDeserializationFails_ThrowsLLMConnectException()
    {
        // Arrange
        var invalidJson = "{ invalid: }"; // Malformed JSON
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, invalidJson);
        var provider = new AnthropicProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Be("Failed to deserialize response");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to deserialize response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_WhenValidRequest_YieldsChatChunks()
    {
        // Arrange
        var sseContent = @"
            event: content_block_delta
            data: {""delta"":{""text"":""Hello""}}

            event: content_block_delta
            data: {""delta"":{""text"":"" world""}}

            event: message_stop
            data: {}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new AnthropicProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        // Act
        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        // Assert
        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Be("Hello");
        chunks[0].IsComplete.Should().BeFalse();
        chunks[1].Content.Should().Be(" world");
        chunks[1].IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task StreamAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
        var provider = new AnthropicProvider(httpClient, _options);

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
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Contain("Internal Server Error");
    }

    [Fact]
    public async Task StreamAsync_WhenCancellationRequested_StopsStreaming()
    {
        // Arrange
        var sseContent = @"
            event: content_block_delta
            data: {""delta"":{""text"":""Hello""}}

            event: content_block_delta
            data: {""delta"":{""text"":"" world""}}

            event: message_stop
            data: {}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new AnthropicProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        using var cts = new CancellationTokenSource();
        var enumerateTask = Task.Run(async () =>
        {
            var chunks = new List<ChatChunk>();
            await foreach (var chunk in provider.StreamAsync(request, cts.Token))
            {
                chunks.Add(chunk);
                cts.Cancel(); // Cancel after first chunk
            }
            return chunks;
        });

        var chunks = await enumerateTask;

        // Assert: Should have at least the first chunk, but not necessarily the second
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be("Hello");
    }

    [Fact]
    public async Task StreamAsync_WhenResponseContainsOnlyFinishEvent_ReturnsNoChunks()
    {
        // Arrange
        var sseContent = @"
            event: message_stop
            data: {}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new AnthropicProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        // Act
        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        // Assert
        chunks.Should().BeEmpty();
    }
}