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
    private readonly Mock<ILogger<GoogleProvider>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _generalOptions;
    private readonly LLMConnectEndpointOptions _endpointOptions;

    public GoogleProviderTests()
    {
        _loggerMock = new Mock<ILogger<GoogleProvider>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "gemini-3.5-flash"
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
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
        };
    }

    [Fact]
    public async Task ChatAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "candidates": [{ "content": { "parts": [{ "text": "Hello" }] }, "finishReason": "STOP" }],
            "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 5 }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new GoogleProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.FinishReason.Should().Be("STOP");
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
        var provider = new GoogleProvider(httpClient, _generalOptions, _endpointOptions);

        Func<Task> act = async () => await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Google");
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
                    Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ok\"}]}}]}")
                });
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
        };
        var provider = new GoogleProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act & Assert
        Func<Task> act = async () => await provider.ChatAsync(request, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var sse = """
            data: {"candidates":[{"content":{"parts":[{"text":"Hello"}]}}]}

            data: {"candidates":[{"content":{"parts":[{"text":" world"}]}}]}

            data: {"candidates":[{"finishReason":"STOP"}]}
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new GoogleProvider(httpClient, _generalOptions, _endpointOptions);

        var chunks = await provider.StreamAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } }).ToListAsync();

        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
        chunks[2].IsComplete.Should().BeTrue();
        chunks[2].FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task GetEmbeddingAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "embedding": { "values": [0.5, 0.6, 0.7] }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new GoogleProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.GetEmbeddingAsync(new EmbeddingRequest { Text = "Hello" });

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.5f, 0.6f, 0.7f });
        result.Model.Should().Be("google");
        result.Usage.Should().BeNull();
    }
}