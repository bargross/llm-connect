using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace LLMConnect.Tests;

public class LLMConnectClientTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public LLMConnectClientTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
    }

    private T? GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)field?.GetValue(obj);
    }

    private void SetPrivateField<T>(object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    // ---------- Constructors ----------

    [Fact]
    public void Constructor_WithOptionsOnly_CreatesHttpClientAndOwnsIt()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            MaxRetries = 3
        };

        // Act
        var client = new LLMConnectClient(options);

        // Assert
        var ownsHttpClient = GetPrivateField<bool>(client, "_ownsHttpClient");
        ownsHttpClient.Should().BeTrue();

        var httpClient = GetPrivateField<HttpClient>(client, "_httpClient");
        httpClient.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithOptionsOnly_CreatesLogger()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };

        // Act
        var client = new LLMConnectClient(options);

        // Assert
        var logger = GetPrivateField<ILogger<LLMConnectClient>>(client, "_logger");
        logger.Should().NotBeNull();

        _loggerFactoryMock.Verify(x => x.CreateLogger(It.Is<string>(s => s == typeof(LLMConnectClient).FullName)), Times.Once);
    }

    [Fact]
    public void Constructor_WithHttpClient_DoesNotOwnClient()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var httpClient = new HttpClient();

        // Act
        var client = new LLMConnectClient(options, httpClient);

        // Assert
        var ownsHttpClient = GetPrivateField<bool>(client, "_ownsHttpClient");
        ownsHttpClient.Should().BeFalse();

        var clientHttpClient = GetPrivateField<HttpClient>(client, "_httpClient");
        clientHttpClient.Should().BeSameAs(httpClient);
    }

    [Fact]
    public void Constructor_WithHttpClient_LogsWarning()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var httpClient = new HttpClient();

        // Act
        var client = new LLMConnectClient(options, httpClient);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using a user-supplied HttpClient")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_CreatesClientAndOwnsIt()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var expectedClient = new HttpClient();
        httpClientFactoryMock
            .Setup(x => x.CreateClient("LLMConnect"))
            .Returns(expectedClient);

        // Act
        var client = new LLMConnectClient(options, httpClientFactoryMock.Object);

        // Assert
        var ownsHttpClient = GetPrivateField<bool>(client, "_ownsHttpClient");
        ownsHttpClient.Should().BeTrue();

        var clientHttpClient = GetPrivateField<HttpClient>(client, "_httpClient");
        clientHttpClient.Should().BeSameAs(expectedClient);
    }

    // ---------- Dispose ----------

    [Fact]
    public void Dispose_WhenOwnsClient_DisposesHttpClient()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new LLMConnectClient(options);
        var httpClient = GetPrivateField<HttpClient>(client, "_httpClient");
        httpClient.Should().NotBeNull();

        // Act
        client.Dispose();

        // Assert
        // The HttpClient is disposed; we can check that it throws ObjectDisposedException when used.
        var disposed = false;
        try
        {
            // Access a property that throws if disposed
            var baseAddress = httpClient.BaseAddress;
        }
        catch (ObjectDisposedException)
        {
            disposed = true;
        }
        disposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenNotOwnsClient_DoesNotDisposeHttpClient()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var httpClient = new HttpClient();
        var client = new LLMConnectClient(options, httpClient);

        // Act
        client.Dispose();

        // Assert
        // The HttpClient should still be usable (not disposed).
        httpClient.BaseAddress.Should().BeNull(); // No exception thrown
    }

    // ---------- Delegation to Provider ----------

    [Fact]
    public async Task ChatAsync_DelegatesToProvider()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new LLMConnectClient(options);

        var mockProvider = new Mock<ILLMProvider>();
        var expectedResponse = new ChatResponse { Content = "Hello from mock" };
        mockProvider
            .Setup(x => x.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Replace the private _provider field with the mock
        SetPrivateField(client, "_provider", mockProvider.Object);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        var result = await client.ChatAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        mockProvider.Verify(x => x.ChatAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamAsync_DelegatesToProvider()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new LLMConnectClient(options);

        var mockProvider = new Mock<ILLMProvider>();
        var expectedChunks = new List<ChatChunk>
        {
            new ChatChunk { Content = "Hello" },
            new ChatChunk { Content = " world" }
        };
        var asyncEnumerable = expectedChunks.ToAsyncEnumerable();
        mockProvider
            .Setup(x => x.StreamAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(asyncEnumerable);

        // Replace the private _provider field with the mock
        SetPrivateField(client, "_provider", mockProvider.Object);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };

        // Act
        var result = client.StreamAsync(request);

        // Assert
        result.Should().NotBeNull();
        // Enumerate to verify it's the same enumerable
        var chunks = await result.ToListAsync();
        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Be("Hello");
        chunks[1].Content.Should().Be(" world");
        mockProvider.Verify(x => x.StreamAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}