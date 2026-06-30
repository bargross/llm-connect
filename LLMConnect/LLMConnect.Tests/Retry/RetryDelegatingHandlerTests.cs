using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace LLMConnect.Tests.Retry;

public class RetryDelegatingHandlerTests
{
    private readonly Mock<ILogger> _loggerMock;

    public RetryDelegatingHandlerTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public async Task SendAsync_WhenInnerHandlerSucceeds_ReturnsResponse()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var innerHandlerMock = new Mock<HttpMessageHandler>();
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse)
            .Verifiable();

        var handler = new RetryDelegatingHandler(3, _loggerMock.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Should().Be(expectedResponse);
        innerHandlerMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WhenInnerHandlerFailsWithHttpRequestException_RetriesAndThrowsAfterMaxRetries()
    {
        // Arrange
        var innerHandlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                callCount++;
                throw new HttpRequestException("Simulated transient failure");
            })
            .Verifiable();

        var handler = new RetryDelegatingHandler(3, _loggerMock.Object);
        handler.InnerHandler = innerHandlerMock.Object;

        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com");

        // Act
        Func<Task> act = async () => await httpClient.SendAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Simulated transient failure");

        callCount.Should().Be(4); // initial + 3 retries
        innerHandlerMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WhenInnerHandlerReturnsInternalServerError_RetriesAndThrowsAfterMaxRetries()
    {
        // Arrange
        var innerHandlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                callCount++;
                if (callCount <= 3)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            })
            .Verifiable();

        var handler = new RetryDelegatingHandler(3, _loggerMock.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert: After retries, eventually succeeds
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(4);
        innerHandlerMock.Verify();
    }

    [Fact]
    public async Task SendAsync_PropagatesCancellationTokenToInnerHandler()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel the token

        var innerHandlerMock = new Mock<HttpMessageHandler>();
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Throws<OperationCanceledException>(); // Simulate cancellation

        var handler = new RetryDelegatingHandler(3, _loggerMock.Object);
        handler.InnerHandler = innerHandlerMock.Object;

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com");

        // Act
        var act = async () => await client.SendAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        innerHandlerMock.Verify();
    }
}