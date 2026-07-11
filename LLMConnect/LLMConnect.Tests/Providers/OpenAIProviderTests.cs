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
    private readonly Mock<ILogger<OpenAIProvider>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _generalOptions;
    private readonly LLMConnectEndpointOptions _endpointOptions;

    public OpenAIProviderTests()
    {
        _loggerMock = new Mock<ILogger<OpenAIProvider>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "gpt-4"
        };
        _endpointOptions = new LLMConnectEndpointOptions();
    }

    private HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
    }

    // ---------- ChatAsync ----------

    [Fact]
    public async Task ChatAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "id": "chatcmpl-123",
            "model": "gpt-4",
            "choices": [{ "message": { "content": "Hello" }, "finish_reason": "stop" }],
            "usage": { "prompt_tokens": 10, "completion_tokens": 5 }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        Func<Task> act = async () => await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("OpenAI");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test\",\"choices\":[{\"message\":{\"content\":\"ok\"}}]}")
                });
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act & Assert
        Func<Task> act = async () => await provider.ChatAsync(request, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---------- StreamAsync ----------

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"Hello"}}]}

            data: {"choices":[{"delta":{"content":" world"}}]}

            data: [DONE]
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        var chunks = await provider.StreamAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } }).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
    }

    [Fact]
    public async Task StreamAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Rate limit""}}";
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        Func<Task> act = async () =>
        {
            var enumerator = provider.StreamAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } }).GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        };

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("OpenAI");
        ex.Which.Message.Should().Be("Rate limit");
    }

    // ---------- Embeddings ----------

    [Fact]
    public async Task GetEmbeddingAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "data": [{ "embedding": [0.1, 0.2, 0.3] }],
            "model": "text-embedding-ada-002",
            "usage": { "prompt_tokens": 10, "total_tokens": 10 }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OpenAIProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.GetEmbeddingAsync(new EmbeddingRequest { Text = "Hello" });

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        result.Model.Should().Be("text-embedding-ada-002");
        result.Usage.InputTokens.Should().Be(10);
    }
}