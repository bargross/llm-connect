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
        var client = CreateClientWithRetry(2,
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Func<Task> act = async () => await client.GetAsync("http://test.com");

        await act.Should().ThrowAsync<HttpRequestException>();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retry")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));
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
        var client = CreateClientWithRetry(0,
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Func<Task> act = async () => await client.GetAsync("http://test.com");

        await act.Should().ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyNoOtherCalls();
    }
}