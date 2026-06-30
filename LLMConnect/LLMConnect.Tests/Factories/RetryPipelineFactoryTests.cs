using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Factories;

public class RetryPipelineFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;

    public RetryPipelineFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public async Task Create_WhenMaxRetriesIsZero_DoesNotRetry()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(0, _loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        callCount.Should().Be(1); // Only one attempt, no retry

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenRetryableStatusCode_RetriesAndSucceeds()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(3, _loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            callCount++;
            if (callCount <= 2)
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(3); // 1 initial + 2 retries

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 0 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 1 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenRetryableException_RetriesAndSucceeds()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(3, _loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            callCount++;
            if (callCount <= 2)
                throw new HttpRequestException("Network error");
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(3);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 0 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 1 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenRetriesExhausted_ThrowsLastException()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(2, _loggerMock.Object);
        var callCount = 0;

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync<HttpResponseMessage>(async (ct) =>
            {
                callCount++;
                throw new HttpRequestException("Persistent error");
            }, CancellationToken.None);
        };

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Persistent error");
        callCount.Should().Be(3); // 1 initial + 2 retries

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 0 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 1 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenNonRetryableStatusCode_DoesNotRetry()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(3, _loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        callCount.Should().Be(1);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenCancellationTokenIsCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(3, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await pipeline.ExecuteAsync(async (ct) =>
        {
            await Task.Delay(100, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        // No retry logging should occur
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenRetrySucceedsOnSecondAttempt_DoesNotRetryMore()
    {
        // Arrange
        var pipeline = RetryPipelineFactory.Create(5, _loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            callCount++;
            if (callCount == 1)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(2);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 0 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry 1 after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Create_WhenLoggerIsNull_DoesNotThrow()
    {
        // Act
        var pipeline = RetryPipelineFactory.Create(3, null);

        // Assert
        pipeline.Should().NotBeNull();

        // Verify it works by executing a simple delegate
        Func<Task> act = async () =>
        {
            var result = await pipeline.ExecuteAsync<HttpResponseMessage>(async (ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }, CancellationToken.None);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        };

        act.Should().NotThrowAsync();
    }
}