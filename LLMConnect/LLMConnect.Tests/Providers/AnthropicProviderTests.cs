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
    private readonly Mock<ILogger<AnthropicProvider>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _generalOptions;
    private readonly LLMConnectEndpointOptions _endpointOptions;

    public AnthropicProviderTests()
    {
        _loggerMock = new Mock<ILogger<AnthropicProvider>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "claude-sonnet-5"
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
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };
    }

    [Fact]
    public async Task ChatAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "id": "msg_123",
            "model": "claude-sonnet-5",
            "stop_reason": "end_turn",
            "content": [{ "type": "text", "text": "Hello" }],
            "usage": { "input_tokens": 10, "output_tokens": 5 }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.FinishReason.Should().Be("end_turn");
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
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        Func<Task> act = async () => await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Anthropic");
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
                    Content = new StringContent("{\"id\":\"test\",\"content\":[{\"type\":\"text\",\"text\":\"ok\"}]}")
                });
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act & Assert
        Func<Task> act = async () => await provider.ChatAsync(request, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var sse = """
            event: content_block_delta
            data: {"delta":{"text":"Hello"}}

            event: content_block_delta
            data: {"delta":{"text":" world"}}

            event: message_stop
            data: {}
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var chunks = await provider.StreamAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } }).ToListAsync();

        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
        chunks[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsNotSupportedException()
    {
        var provider = new AnthropicProvider(new HttpClient(), _generalOptions, _endpointOptions);

        Func<Task> act = async () => await provider.GetEmbeddingAsync(new EmbeddingRequest { Text = "Hello" });

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}