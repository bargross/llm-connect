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

public class OpenAIProviderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public OpenAIProviderTests()
    {
        _loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            DefaultModel = "gpt-3.5-turbo",
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
            BaseAddress = new Uri("https://api.openai.com/v1/chat/completions")
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
        // Force the content to be read as a stream
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
            BaseAddress = new Uri("https://api.openai.com/v1/chat/completions")
        };
        return client;
    }

    [Fact]
    public async Task ChatAsync_WhenValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseJson = @"
        {
            ""id"": ""chatcmpl-123"",
            ""model"": ""gpt-3.5-turbo"",
            ""created"": 1677651234,
            ""choices"": [
                {
                    ""index"": 0,
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Hello, world!""
                    },
                    ""finish_reason"": ""stop""
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 5,
                ""total_tokens"": 15
            }
        }";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, responseJson);
        var provider = new OpenAIProvider(httpClient, _options);

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
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.Unauthorized, errorJson);
        var provider = new OpenAIProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_WhenDeserializationFails_ThrowsLLMConnectException()
    {
        // Arrange
        var invalidJson = "{ invalid: }"; // Malformed JSON
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, invalidJson);
        var provider = new OpenAIProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
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
        var ndjsonContent = @"
            data: {""choices"":[{""delta"":{""content"":""Hello""}}]}

            data: {""choices"":[{""delta"":{""content"":"" world""}}]}

            data: [DONE]
        ";
        var httpClient = CreateHttpClientWithStreamingResponse(ndjsonContent);
        var provider = new OpenAIProvider(httpClient, _options);

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
        var provider = new OpenAIProvider(httpClient, _options);

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
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Contain("Internal Server Error");
    }

    [Fact]
    public async Task StreamAsync_WhenCancellationRequested_StopsStreaming()
    {
        // Arrange
        var ndjsonContent = @"
            data: {""choices"":[{""delta"":{""content"":""Hello""}}]}

            data: {""choices"":[{""delta"":{""content"":"" world""}}]}

            data: [DONE]
        ";
        var httpClient = CreateHttpClientWithStreamingResponse(ndjsonContent);
        var provider = new OpenAIProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        using var cts = new CancellationTokenSource();
        // Cancel after a short delay
        var enumerateTask = Task.Run(async () =>
        {
            var chunks = new List<ChatChunk>();
            await foreach (var chunk in provider.StreamAsync(request, cts.Token))
            {
                chunks.Add(chunk);
                // Cancel after first chunk
                cts.Cancel();
            }
            return chunks;
        });

        // Wait for the task to complete
        var chunks = await enumerateTask;

        // Assert: Should have at least the first chunk, but not necessarily the second due to cancellation
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be("Hello");
    }
}