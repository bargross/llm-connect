using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Runtime.CompilerServices;

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
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-anthropic-key",
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "claude-3-5-sonnet-20241022"
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
            .ReturnsAsync(response)
            .Verifiable();

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };
        return client;
    }

    private HttpResponseMessage CreateSuccessResponse(string jsonContent)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage CreateErrorResponse(string errorJson, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
    }

    // ---------- ChatAsync ----------

    [Fact]
    public async Task ChatAsync_WithValidResponse_ReturnsChatResponse()
    {
        var responseJson = @"
        {
            ""id"": ""msg_123"",
            ""model"": ""claude-3-5-sonnet-20241022"",
            ""stop_reason"": ""end_turn"",
            ""content"": [{ ""type"": ""text"", ""text"": ""Hello from Anthropic!"" }],
            ""usage"": { ""input_tokens"": 10, ""output_tokens"": 5 }
        }";
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        var result = await provider.ChatAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from Anthropic!");
        result.FinishReason.Should().Be("end_turn");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
        result.Model.Should().Be("claude-3-5-sonnet-20241022");
    }

    [Fact]
    public async Task ChatAsync_WithErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        var httpClient = CreateMockHttpClient(CreateErrorResponse(errorJson, HttpStatusCode.Unauthorized));
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_ValidationFails_ThrowsArgumentException()
    {
        var httpClient = CreateMockHttpClient(CreateSuccessResponse("{}"));
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message>()
        };

        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one message*");
    }

    // ---------- StreamAsync ----------

    [Fact]
    public async Task StreamAsync_WithValidStream_ReturnsChatChunks()
    {
        var sseContent = """
            event: content_block_delta
            data: {"delta":{"text":"Hello"}}

            event: content_block_delta
            data: {"delta":{"text":" world"}}

            event: message_stop
            data: {}
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
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

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
    }

    [Fact]
    public async Task StreamAsync_WithErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        var errorResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse)
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        Func<Task> act = async () =>
        {
            var enumerator = provider.StreamAsync(request, CancellationToken.None).GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        };

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    private static async IAsyncEnumerable<StreamEvent> ReadCustomEventsAsync(Stream stream, [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (line.StartsWith("custom: "))
                yield return new StreamEvent(null, line.Substring(8));
        }
    }

    // ---------- GetEmbeddingAsync ----------

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsNotSupportedException()
    {
        var httpClient = new HttpClient();
        var provider = new AnthropicProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new EmbeddingRequest { Text = "Hello" };

        Func<Task> act = async () => await provider.GetEmbeddingAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}