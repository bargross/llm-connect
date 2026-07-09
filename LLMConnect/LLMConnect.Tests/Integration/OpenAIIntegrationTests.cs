using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LLMConnect.Tests.Integration;

public class OpenAIIntegrationTests : IntegrationTestBase
{
    public OpenAIIntegrationTests() : base(ProviderType.OpenAI, "/chat/completions") { }

    // ---------- Stubs ----------
    private void StubChat(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    private void StubStream(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"data: {chunk}");
            sb.AppendLine();
        }
        sb.AppendLine("data: [DONE]");
        sb.AppendLine();

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sb.ToString()));
    }

    private void StubEmbeddings(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/embeddings").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    // ---------- Tests ----------

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "id": "chatcmpl-123",
            "model": "gpt-3.5-turbo",
            "created": 1677651234,
            "choices": [{ "message": { "content": "Hello from OpenAI!" }, "finish_reason": "stop" }],
            "usage": { "prompt_tokens": 10, "completion_tokens": 5 }
        }
        """;
        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from OpenAI!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""choices"":[{""delta"":{""content"":""Hello""}}]}",
            @"{""choices"":[{""delta"":{""content"":"" world""}}]}"
        };

        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
    }

    [Fact]
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":{""message"":""Invalid API key""}}", HttpStatusCode.Unauthorized);

        var act = async () => await _client.ChatAsync(CreateChatRequest());

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("OpenAI");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = """
        {
            "choices": [{ "message": { "content": "Retry worked!" } }],
            "usage": { "prompt_tokens": 5, "completion_tokens": 2 }
        }
        """;

        StubRetryScenario("/chat/completions", fail, success);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Content.Should().Be("Retry worked!");
    }

    [Fact]
    public async Task EmbeddingsAsync_ValidResponse_ReturnsEmbeddingResponse()
    {
        var json = """
        {
            "data": [{ "embedding": [0.1, 0.2, 0.3] }],
            "model": "text-embedding-3-small",
            "usage": { "prompt_tokens": 10, "total_tokens": 10 }
        }
        """;
        StubEmbeddings(json);

        var result = await _client.GetEmbeddingAsync(CreateEmbeddingRequest());

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        result.Model.Should().Be("text-embedding-3-small");
        result.Usage.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(10);
    }
}