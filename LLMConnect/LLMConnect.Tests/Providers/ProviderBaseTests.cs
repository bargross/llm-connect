using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
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

    // ---------- Test Subclass (public) ----------
    internal class TestProvider : ProviderBase<TestProvider>
    {
        public TestProvider(LLMConnectGeneralOptions options) : base(options) { }

        // Expose protected methods for testing
        public new async Task<string> ExtractErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
            => await base.ExtractErrorMessage(response, cancellationToken);

        public new async Task LogAndThrow(ProviderType? providerType, HttpResponseMessage response, CancellationToken cancellationToken)
            => await base.LogAndThrow(providerType, response, cancellationToken);

        public new async Task<TResponse?> DeserializeResponseAsync<TProviderResponse, TResponse>(
            HttpResponseMessage response,
            ProviderType? provider,
            Func<TProviderResponse?, TResponse?> toChatResponse,
            CancellationToken cancellationToken)
            => await base.DeserializeResponseAsync(response, provider, toChatResponse, cancellationToken);

        public new string GetUrlRelativePath(
            LLMConnectEndpointOptions options,
            int type,
            bool isStreaming,
            string? model = null,
            ILogger? logger = null)
            => base.GetUrlRelativePath(options, (QueryType)type, isStreaming, model, logger);
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
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""", Encoding.UTF8, "application/json")
        };

        Func<Task> act = async () => await _provider.LogAndThrow(ProviderType.OpenAI, response, CancellationToken.None);

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
    }

    // ---------- DeserializeResponseAsync Tests ----------
    private class TestProviderResponse
    {
        public string? Text { get; set; }
    }

    private static ChatResponse? MapProviderResponse(TestProviderResponse? response)
    {
        if (response == null) return null;
        return new ChatResponse { Content = response.Text ?? string.Empty };
    }

    [Fact]
    public async Task DeserializeResponseAsync_ValidResponse_ReturnsMappedResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"Text":"Hello"}""", Encoding.UTF8, "application/json")
        };

        var result = await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            MapProviderResponse,
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }

    [Fact]
    public async Task DeserializeResponseAsync_WhenMappingReturnsNull_ThrowsLLMConnectException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"Text":"Hello"}""", Encoding.UTF8, "application/json")
        };

        Func<TestProviderResponse?, ChatResponse?> mapNull = (resp) => null;

        Func<Task> act = async () => await _provider.DeserializeResponseAsync<TestProviderResponse, ChatResponse>(
            response,
            ProviderType.OpenAI,
            mapNull,
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Failed to deserialize response.");
    }

    // ---------- GetUrlRelativePath Tests ----------
    [Fact]
    public void GetUrlRelativePath_NonStreaming_ReturnsRelativePath()
    {
        var endpointOpts = new LLMConnectEndpointOptions();

        var path = _provider.GetUrlRelativePath(endpointOpts, (int)QueryType.Chat, isStreaming: false);

        path.Should().Be("chat/completions");
    }

    [Fact]
    public void GetUrlRelativePath_ForGoogleProvider_WithoutModel_ThrowsLLMConnectException()
    {
        var googleOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var googleProvider = new TestProvider(googleOptions);
        var endpointOpts = new LLMConnectEndpointOptions();

        Action act = () => googleProvider.GetUrlRelativePath(endpointOpts, (int)QueryType.Chat, isStreaming: false, model: null);

        act.Should().Throw<LLMConnectException>()
            .WithMessage("Model must be specified for Google provider.");
    }

    [Fact]
    public void GetUrlRelativePath_ForGoogleProvider_SubstitutesModelPlaceholder()
    {
        var googleOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Google,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var googleProvider = new TestProvider(googleOptions);
        var endpointOpts = new LLMConnectEndpointOptions();

        var path = googleProvider.GetUrlRelativePath(endpointOpts, (int)QueryType.Chat, isStreaming: false, model: "gemini-3.5-flash");

        path.Should().Be("models/gemini-3.5-flash:generateContent");
    }

    [Fact]
    public void GetUrlRelativePath_ForNonGoogleProvider_WithNullModel_DoesNotThrow()
    {
        var endpointOpts = new LLMConnectEndpointOptions();

        Action act = () => _provider.GetUrlRelativePath(endpointOpts, (int)QueryType.Chat, isStreaming: false, model: null);

        act.Should().NotThrow();
    }
}