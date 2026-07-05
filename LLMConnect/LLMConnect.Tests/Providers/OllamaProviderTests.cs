using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "llama3.2"
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
            BaseAddress = new Uri("http://localhost:11434/api/")
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
            ""model"": ""llama3.2"",
            ""message"": { ""role"": ""assistant"", ""content"": ""Hello from Ollama!"" },
            ""done"": true,
            ""done_reason"": ""stop"",
            ""eval_count"": 5,
            ""prompt_eval_count"": 10
        }";
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        var result = await provider.ChatAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from Ollama!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(5);
        result.Usage.OutputTokens.Should().Be(10);
        result.Model.Should().Be("llama3.2");
    }

    [Fact]
    public async Task ChatAsync_WithErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":""Internal server error""}";
        var httpClient = CreateMockHttpClient(CreateErrorResponse(errorJson, HttpStatusCode.InternalServerError));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        Func<Task> act = async () => await provider.ChatAsync(request, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task ChatAsync_WithCustomDeserializer_UsesCustomDeserializer()
    {
        var customJson = @"{""custom"":""Custom response""}";
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(customJson));
        var endpointOpts = new LLMConnectEndpointOptions
        {
            Endpoint = "custom",
            ChatResponseDeserializer = async (json, ct) =>
            {
                await Task.CompletedTask;
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("custom").GetString();
                return new ChatResponse { Content = text ?? string.Empty };
            }
        };
        var provider = new OllamaProvider(httpClient, _generalOptions, endpointOpts);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        var result = await provider.ChatAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Custom response");
    }

    [Fact]
    public async Task ChatAsync_ValidationFails_ThrowsArgumentException()
    {
        var httpClient = CreateMockHttpClient(CreateSuccessResponse("{}"));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

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
        var ndjsonContent = """
            {"message":{"content":"Hello"},"done":false}
            {"message":{"content":" world"},"done":false}
            {"message":{"content":"!"},"done":true}
            """;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ndjsonContent, Encoding.UTF8, "application/x-ndjson")
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
            BaseAddress = new Uri("http://localhost:11434/api/")
        };
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Hello");
        chunks[0].IsComplete.Should().BeFalse();
        chunks[1].Content.Should().Be(" world");
        chunks[1].IsComplete.Should().BeFalse();
        chunks[2].Content.Should().Be("!");
        chunks[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task StreamAsync_WithErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":""Internal server error""}";
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
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
            BaseAddress = new Uri("http://localhost:11434/api/")
        };
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

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
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task StreamAsync_WithCustomReadersAndParsers_UsesCustom()
    {
        var customContent = "custom: Hello\ncustom: world\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(customContent, Encoding.UTF8, "text/plain")
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
            BaseAddress = new Uri("http://localhost:11434/api/")
        };

        // Custom reader and parser
        var customReader = new Mock<IStreamEventReader>();
        customReader
            .Setup(x => x.ReadEventsAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream stream, CancellationToken ct) => ReadCustomEventsAsync(stream, ct));

        var customParser = new Mock<IStreamChunkParser>();
        customParser
            .Setup(x => x.Parse(It.IsAny<StreamEvent>()))
            .Returns<StreamEvent>(evt => new ChatChunk { Content = evt.Data, IsComplete = false });

        var endpointOpts = new LLMConnectEndpointOptions
        {
            Endpoint = "custom",
            CustomStreamEventReaderFactory = () => customReader.Object,
            CustomStreamChunkParserFactory = () => customParser.Object
        };

        var provider = new OllamaProvider(httpClient, _generalOptions, endpointOpts);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Say hello") }
        };

        var chunks = await provider.StreamAsync(request, CancellationToken.None).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be("world");
        customReader.Verify(x => x.ReadEventsAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        customParser.Verify(x => x.Parse(It.IsAny<StreamEvent>()), Times.Exactly(2));
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
    public async Task GetEmbeddingAsync_WithValidResponse_ReturnsEmbeddingResponse()
    {
        var responseJson = @"
        {
            ""embedding"": [0.1, 0.2, 0.3]
        }";
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new EmbeddingRequest { Text = "Hello" };

        var result = await provider.GetEmbeddingAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        result.Model.Should().Be("ollama");
        result.Usage.Should().BeNull();
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":""Internal server error""}";
        var httpClient = CreateMockHttpClient(CreateErrorResponse(errorJson, HttpStatusCode.InternalServerError));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new EmbeddingRequest { Text = "Hello" };

        Func<Task> act = async () => await provider.GetEmbeddingAsync(request, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithCustomDeserializer_UsesCustomDeserializer()
    {
        var customJson = @"{""custom"": [1.0, 2.0]}";
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(customJson));
        var endpointOpts = new LLMConnectEndpointOptions
        {
            Endpoint = "custom",
            EmbeddingResponseDeserializer = async (json, ct) =>
            {
                await Task.CompletedTask;
                using var doc = JsonDocument.Parse(json);
                var values = doc.RootElement.GetProperty("custom").EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
                return new EmbeddingResponse { Embedding = values };
            }
        };
        var provider = new OllamaProvider(httpClient, _generalOptions, endpointOpts);

        var request = new EmbeddingRequest { Text = "Hello" };

        var result = await provider.GetEmbeddingAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 1.0f, 2.0f });
    }

    [Fact]
    public async Task GetEmbeddingAsync_ValidationFails_ThrowsArgumentException()
    {
        var httpClient = CreateMockHttpClient(CreateSuccessResponse("{}"));
        var provider = new OllamaProvider(httpClient, _generalOptions, _endpointOptions);

        var request = new EmbeddingRequest { Text = "" }; // Empty text fails validation

        Func<Task> act = async () => await provider.GetEmbeddingAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Embedding text cannot be null or whitespace*");
    }
}