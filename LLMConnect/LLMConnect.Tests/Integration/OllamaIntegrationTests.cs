using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace LLMConnect.Tests.Integration;

public class OllamaIntegrationTests : IntegrationTestBase
{
    public OllamaIntegrationTests()
        : base(ProviderType.Ollama, requiresApiKey: false)
    {
    }

    // ---------- Stubs ----------
    private void StubChat(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    private void StubStream(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
            sb.AppendLine(chunk);

        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(sb.ToString()));
    }

    private void StubEmbeddings(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/api/embed").UsingPost())
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
            "model": "llama3.2",
            "message": { "role": "assistant", "content": "Hello from Ollama!" },
            "done": true,
            "done_reason": "stop",
            "eval_count": 5,
            "prompt_eval_count": 10
        }
        """;
        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Content.Should().Be("Hello from Ollama!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(5);
        result.Usage.OutputTokens.Should().Be(10);
        result.Model.Should().Be("llama3.2");
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""message"":{""content"":""Hello""},""done"":false}",
            @"{""message"":{""content"":"" world""},""done"":false}",
            @"{""message"":{""content"":""!""},""done"":true}"
        };
        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();

        result.Should().HaveCount(3);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
        result[2].Content.Should().Be("!");
        result[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":""Internal server error""}", HttpStatusCode.InternalServerError);

        Func<Task> act = async () => await _client.ChatAsync(CreateChatRequest());

        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Ollama");
        ex.Which.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":""Rate limit""}";
        var success = """
        {
            "message": { "content": "Retry worked!" },
            "done": true,
            "eval_count": 5,
            "prompt_eval_count": 2
        }
        """;
        StubRetryScenario("/api/chat", fail, success);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Content.Should().Be("Retry worked!");
    }

    [Fact]
    public async Task EmbeddingsAsync_ValidResponse_ReturnsEmbeddingResponse()
    {
        var json = """
        {
            "embedding": [1.0, 2.0, 3.0]
        }
        """;

        StubEmbeddings(json);

        var result = await _client.GetEmbeddingAsync(CreateEmbeddingRequest());

        result.Should().NotBeNull();
        result.Embedding.Should().BeEquivalentTo(new float[] { 1.0f, 2.0f, 3.0f });
        result.Model.Should().Be("ollama");
        result.Usage.Should().BeNull();
    }
}