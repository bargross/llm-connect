using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect.Tests.Providers;

public class ProviderBaseTests
{
    private readonly Mock<ILogger<TestProvider>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;
    private readonly TestProvider _provider;

    public ProviderBaseTests()
    {
        _loggerMock = new Mock<ILogger<TestProvider>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };

        _provider = new TestProvider(_options);
    }

    // ---------- Test Subclass ----------

    private class TestProviderResponse
    {
        public string? Text { get; set; }
    }

    private static ChatResponse? MapProviderResponse(TestProviderResponse? response)
    {
        if (response == null) return null;
        return new ChatResponse { Content = response.Text ?? string.Empty };
    }

    // ---------- ExtractErrorMessage Tests ----------

    [Fact]
    public async Task ExtractErrorMessage_OpenAIErrorFormat_ReturnsMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ExtractErrorMessage_AnthropicErrorFormat_ReturnsMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid request"}}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be("Invalid request");
    }

    [Fact]
    public async Task ExtractErrorMessage_GoogleErrorFormat_ReturnsMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"API key not valid"}}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be("API key not valid");
    }

    [Fact]
    public async Task ExtractErrorMessage_ErrorAsString_ReturnsString()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"Internal server error"}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be("Internal server error");
    }

    [Fact]
    public async Task ExtractErrorMessage_TopLevelMessage_ReturnsMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Service unavailable"}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be("Service unavailable");
    }

    [Fact]
    public async Task ExtractErrorMessage_NonJsonBody_ReturnsStatusCodeAndRawBody()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be($"HTTP error: {HttpStatusCode.InternalServerError} - Internal Server Error");
    }

    [Fact]
    public async Task ExtractErrorMessage_EmptyBody_ReturnsStatusCodeOnly()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be($"HTTP error: {HttpStatusCode.NotFound} - ");
    }

    [Fact]
    public async Task ExtractErrorMessage_MalformedJson_ReturnsStatusCodeAndRawBody()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{invalid json", Encoding.UTF8, "application/json")
        };

        var result = await _provider.ExtractErrorMessage(response, CancellationToken.None);

        result.Should().Be($"HTTP error: {HttpStatusCode.BadRequest} - {await response.Content.ReadAsStringAsync()}");
    }

    // ---------- LogAndThrow Tests ----------

    [Fact]
    public async Task LogAndThrow_LogsErrorAndThrowsLLMConnectException()
    {
        var providerType = ProviderType.OpenAI;
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        Func<Task> act = async () => await _provider.LogAndThrow(providerType, response, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Invalid API key");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAndThrow_WhenLoggerIsNull_StillThrows()
    {
        var optionsWithoutLogger = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = null
        };
        var provider = new TestProvider(optionsWithoutLogger);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        Func<Task> act = async () => await provider.LogAndThrow(ProviderType.OpenAI, response, CancellationToken.None);

        await act.Should().ThrowAsync<LLMConnectException>()
            .WithMessage("Invalid API key");
        // No log verification because logger is null
    }

    // ---------- DeserializeResponseAsync Tests ----------

    [Fact]
    public async Task DeserializeResponseAsync_WithCustomDeserializer_UsesCustom()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"custom":"Hello from custom"}""", Encoding.UTF8, "application/json")
        };

        bool customEvaluator() => true;
        Func<string, Task<ChatResponse?>> customDeserializer = async (json) =>
        {
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("custom").GetString();
            return new ChatResponse { Content = text ?? string.Empty };
        };

        var result = await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            customEvaluator,
            customDeserializer,
            MapProviderResponse,
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from custom");
    }

    [Fact]
    public async Task DeserializeResponseAsync_WithoutCustomDeserializer_UsesStandard()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"Text":"Hello from standard"}""", Encoding.UTF8, "application/json")
        };

        bool customEvaluator() => false;

        var result = await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            customEvaluator,
            null!,
            MapProviderResponse,
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from standard");
    }

    [Fact]
    public async Task DeserializeResponseAsync_WhenCustomDeserializerThrows_WrapsException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"test":"value"}""", Encoding.UTF8, "application/json")
        };

        bool customEvaluator() => true;
        Func<string, Task<ChatResponse?>> customDeserializer = async (json) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Custom deserializer failed");
        };

        Func<Task> act = async () => await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            customEvaluator,
            customDeserializer,
            MapProviderResponse,
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("CustomDeserializer");
        exception.Which.Message.Should().Contain("Custom deserializer failed");
    }

    [Fact]
    public async Task DeserializeResponseAsync_WhenStandardDeserializationFails_ThrowsLLMConnectException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"invalid":"json"}""", Encoding.UTF8, "application/json")
        };

        bool customEvaluator() => false;
        Func<TestProviderResponse?, ChatResponse?> mapWithNull = (response) => null;

        Func<Task> act = async () => await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            customEvaluator,
            null!,
            mapWithNull,
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Failed to deserialize response.");
    }

    // ---------- GetUrl Tests ----------

    [Fact]
    public void GetUrl_WithoutEndpointOrBaseUrl_NonStreaming_ReturnsRelativePath()
    {
        var endpointOpts = new LLMConnectEndpointOptions();

        var url = _provider.GetUrl(endpointOpts, QueryType.Chat, isStreaming: false);

        url.Should().Be("chat/completions");
    }

    [Fact]
    public void GetUrl_WithoutEndpointOrBaseUrl_Streaming_ReturnsAbsoluteUrl_UsingInternalBaseUrl()
    {
        var endpointOpts = new LLMConnectEndpointOptions();

        var url = _provider.GetUrl(endpointOpts, QueryType.Chat, isStreaming: true, internalBaseUrl: "https://api.openai.com/v1/");

        url.Should().Be("https://api.openai.com/v1/chat/completions");
    }

    [Fact]
    public void GetUrl_ForGoogleProvider_WithoutModel_ThrowsLLMConnectException()
    {
        var googleOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var googleProvider = new TestProvider(googleOptions);
        var endpointOpts = new LLMConnectEndpointOptions();

        Action act = () => googleProvider.GetUrl(endpointOpts, QueryType.Chat, isStreaming: false, model: null);

        act.Should().Throw<LLMConnectException>()
            .WithMessage("Model must be specified for Google provider.");
    }

    [Fact]
    public void GetUrl_ForGoogleProvider_SubstitutesModelPlaceholder()
    {
        var googleOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var googleProvider = new TestProvider(googleOptions);
        var endpointOpts = new LLMConnectEndpointOptions();

        var url = googleProvider.GetUrl(endpointOpts, QueryType.Chat, isStreaming: false, model: "gemini-3.5-flash");

        url.Should().Be("models/gemini-3.5-flash:generateContent");
    }

    [Fact]
    public void GetUrl_ForNonGoogleProvider_WithNullModel_DoesNotThrow()
    {
        var endpointOpts = new LLMConnectEndpointOptions();

        Action act = () => _provider.GetUrl(endpointOpts, QueryType.Chat, isStreaming: false, model: null);

        act.Should().NotThrow();
    }

    private class CustomStreamEventReader : IStreamEventReader
    {
        private readonly Stream _stream;

        public CustomStreamEventReader(Stream stream)
        {
            _stream = stream;
        }

        public async IAsyncEnumerable<StreamEvent> ReadEventsAsync(
            Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.StartsWith("custom: "))
                    yield return new StreamEvent(null, line.Substring(8));
            }
        }
    }

    private class CustomStreamChunkParser : IStreamChunkParser
    {
        public ChatChunk? Parse(StreamEvent evt)
        {
            return new ChatChunk { Content = evt.Data, IsComplete = false };
        }
    }
}