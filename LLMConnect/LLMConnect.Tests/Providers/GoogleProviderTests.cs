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

public class GoogleProviderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public GoogleProviderTests()
    {
        _loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-google-api-key",
            DefaultModel = "gemini-2.0-flash",
            LoggerFactory = loggerFactoryMock.Object,
            MaxRetries = 0 // Disable retries for test determinism
        };
    }

    private HttpClient CreateHttpClientWithResponse(HttpStatusCode statusCode, string content, string? baseAddress = null)
    {
        baseAddress ??= "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
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
            BaseAddress = new Uri(baseAddress)
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
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
        };
        return client;
    }

    [Fact]
    public async Task ChatAsync_WhenValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseJson = @"
        {
            ""candidates"": [
                {
                    ""content"": {
                        ""parts"": [
                            { ""text"": ""Hello, world!"" }
                        ]
                    },
                    ""finishReason"": ""STOP""
                }
            ],
            ""usageMetadata"": {
                ""promptTokenCount"": 10,
                ""candidatesTokenCount"": 5
            }
        }";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, responseJson);
        var provider = new GoogleProvider(httpClient, _options);

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
        result.FinishReason.Should().Be("STOP");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var errorJson = @"{""error"":{""message"":""Invalid API key"",""code"":403}}";
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.Forbidden, errorJson);
        var provider = new GoogleProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Google");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_WhenDeserializationFails_ThrowsLLMConnectException()
    {
        // Arrange
        var invalidJson = "{ invalid: }"; // Malformed JSON
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, invalidJson);
        var provider = new GoogleProvider(httpClient, _options);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Google");
        exception.Which.Message.Should().Be("Failed to deserialize response.");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to deserialize response.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_WhenValidRequest_YieldsChatChunks()
    {
        // Arrange
        var sseContent = @"
            data: {""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}

            data: {""candidates"":[{""content"":{""parts"":[{""text"":"" world""}]}}]}

            data: {""candidates"":[{""finishReason"":""STOP""}]}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new GoogleProvider(httpClient, _options);

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
        chunks[0].FinishReason.Should().BeNull();
        chunks[1].Content.Should().Be(" world");
        chunks[1].IsComplete.Should().BeFalse();
        chunks[1].FinishReason.Should().BeNull();
        chunks[2].Content.Should().BeEmpty();
        chunks[2].IsComplete.Should().BeTrue();
        chunks[2].FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task StreamAsync_WhenNonSuccessStatusCode_ThrowsLLMConnectException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
        var provider = new GoogleProvider(httpClient, _options);

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
        exception.Which.Provider.Should().Be("Google");
        exception.Which.Message.Should().Contain("Internal Server Error");
    }

    [Fact]
    public async Task StreamAsync_WhenCancellationRequested_StopsStreaming()
    {
        // Arrange
        var sseContent = @"
            data: {""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}
            data: {""candidates"":[{""content"":{""parts"":[{""text"":"" world""}]}}]}
            data: {""candidates"":[{""finishReason"":""STOP""}]}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new GoogleProvider(httpClient, _options);

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
        chunks[0].IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task StreamAsync_WhenResponseHasFinishReasonOnly_ReturnsCompleteChunk()
    {
        // Arrange
        var sseContent = @"
            data: {""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}
            data: {""candidates"":[{""finishReason"":""MAX_TOKENS""}]}
        ";

        var httpClient = CreateHttpClientWithStreamingResponse(sseContent);
        var provider = new GoogleProvider(httpClient, _options);

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
        chunks[1].Content.Should().BeEmpty();
        chunks[1].IsComplete.Should().BeTrue();
        chunks[1].FinishReason.Should().Be("MAX_TOKENS");
    }
}