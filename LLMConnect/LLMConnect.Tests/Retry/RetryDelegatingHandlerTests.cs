using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;

namespace LLMConnect.Tests.Retry;

public class RetryDelegatingHandlerTests
{
    private readonly Mock<ILogger<RetryDelegatingHandler>> _loggerMock;

    public RetryDelegatingHandlerTests()
    {
        _loggerMock = new Mock<ILogger<RetryDelegatingHandler>>();
    }

    private HttpClient CreateClientWithRetry(int maxRetries, params HttpResponseMessage[] responses)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var sequence = handlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var response in responses)
            sequence.ReturnsAsync(response);

        var retryHandler = new RetryDelegatingHandler(maxRetries, _loggerMock.Object)
        {
            InnerHandler = handlerMock.Object
        };

        return new HttpClient(retryHandler);
    }

    [Fact]
    public async Task SendAsync_WhenAllSuccess_DoesNotRetry()
    {
        var client = CreateClientWithRetry(3,
            new HttpResponseMessage(HttpStatusCode.OK));

        var response = await client.GetAsync("http://test.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_WhenRetryableStatus_RetriesUntilSuccess()
    {
        var client = CreateClientWithRetry(3,
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.OK));

        var response = await client.GetAsync("http://test.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retry")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendAsync_WhenRetryExhausted_ThrowsOriginalException()
    {
        // Arrange: simulate two consecutive exceptions (retryable)
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error 1"))
            .ThrowsAsync(new HttpRequestException("Network error 2"));

        // Set max retries to 1 so total attempts == 2 and matches the two mocked exceptions
        var retryHandler = new RetryDelegatingHandler(2, _loggerMock.Object)
        {
            InnerHandler = handlerMock.Object
        };
        var client = new HttpClient(retryHandler);

        Func<Task> act = async () => await client.GetAsync("http://test.com");

        // Act & Assert
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.And.Message.Should().Be("Network error 2"); // The last exception thrown

        // Verify retry logs
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task SendAsync_WhenInnerHandlerThrows_RetriesOnHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var retryHandler = new RetryDelegatingHandler(2, _loggerMock.Object)
        {
            InnerHandler = handlerMock.Object
        };
        var client = new HttpClient(retryHandler);

        var response = await client.GetAsync("http://test.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendAsync_WithZeroRetries_DoesNotRetry()
    {
        // Arrange: with zero retries, the handler should not retry and should throw the original exception.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var retryHandler = new RetryDelegatingHandler(0, _loggerMock.Object)
        {
            InnerHandler = handlerMock.Object
        };
        var client = new HttpClient(retryHandler);

        Func<Task> act = async () => await client.GetAsync("http://test.com");

        // Act & Assert
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.And.Message.Should().Be("Network error");
        _loggerMock.VerifyNoOtherCalls(); // no retry logs
    }
}