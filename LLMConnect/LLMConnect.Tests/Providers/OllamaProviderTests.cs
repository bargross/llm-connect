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
    private readonly Mock<ILogger<OllamaProvider>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _generalOptions;
    private readonly LLMConnectEndpointOptions _endpointOptions;

    public OllamaProviderTests()
    {
        _loggerMock = new Mock<ILogger<OllamaProvider>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "qwen2.5:7b-instruct"
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
            BaseAddress = new Uri("http://localhost:11434/")
        };
    }

    [Fact]
    public async Task ChatAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "model": "qwen2.5:7b-instruct",
            "message": { "role": "assistant", "content": "Hello" },
            "done": true,
            "done_reason": "stop",
            "eval_count": 5,
            "prompt_eval_count": 10
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(5);   // eval_count -> InputTokens
        result.Usage.OutputTokens.Should().Be(10); // prompt_eval_count -> OutputTokens
    }

    [Fact]
    public async Task ChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":""Internal server error""}";
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        Func<Task> act = async () => await provider.ChatAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } });

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Ollama");
        ex.Which.Message.Should().Contain("Internal server error");
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
                    Content = new StringContent("{\"message\":{\"content\":\"ok\"},\"done\":true}")
                });
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } };

        // Act & Assert
        Func<Task> act = async () => await provider.ChatAsync(request, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var ndjson = """
            {"message":{"content":"Hello"},"done":false}
            {"message":{"content":" world"},"done":false}
            {"message":{"content":"!"},"done":true}
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ndjson, Encoding.UTF8, "application/x-ndjson")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var chunks = await provider.StreamAsync(new ChatRequest { Messages = new List<Message> { new UserMessage("Hi") } }).ToListAsync();

        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
        chunks[2].Content.Should().Be("!");
        chunks[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmbeddingAsync_ValidResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "embedding": [1.0, 2.0, 3.0]
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var httpClient = CreateMockHttpClient(response);
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var result = await provider.GetEmbeddingAsync(new EmbeddingRequest { Text = "Hello" });

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 1f, 2f, 3f });
        result.Model.Should().Be("ollama");
        result.Usage.Should().BeNull();
    }
}