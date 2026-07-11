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
    public OllamaIntegrationTests() : base(ProviderType.Ollama, requiresApiKey: false) { }

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

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "message": { "content": "Hello from Ollama!" },
            "done": true,
            "done_reason": "stop",
            "eval_count": 10,
            "prompt_eval_count": 5
        }
        """;
        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Hello from Ollama!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);   // prompt_eval_count
        result.Usage.OutputTokens.Should().Be(5);    // eval_count
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
        StubRetryScenario("/api/chat", @"{""error"":""Rate limit""}", """
        {
            "message": { "content": "Retry worked!" },
            "done": true,
            "eval_count": 5,
            "prompt_eval_count": 2
        }
        """);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Retry worked!");
    }

    [Fact]
    public async Task ChatAsync_WithTools_ReturnsToolCalls()
    {
        var tool = new Tool
        {
            Name = "get_weather",
            Description = "Get the weather for a location",
            Parameters = new Dictionary<string, JsonSchema> { ["location"] = new JsonSchema { Type = "string" } }
        };
        var json = """
        {
            "message": {
                "content": "",
                "tool_calls": [{ "function": { "name": "get_weather", "arguments": { "location": "Boston" } } }]
            },
            "done": true,
            "done_reason": "stop"
        }
        """;
        StubChat(json);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Weather?") },
            Tools = new List<Tool> { tool }
        };
        var response = await _client.ChatAsync(request);
        response.ToolCalls.Should().HaveCount(1);
        var tc = response.ToolCalls[0];
        tc.Name.Should().Be("get_weather");
        tc.Arguments["location"].ToString().Should().Be("Boston");
    }

    [Fact]
    public async Task ChatAsync_WithToolChoiceRequired_SendsCorrectJson()
    {
        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"message\":{\"content\":\"ok\"},\"done\":true}"));

        var tool = new Tool
        {
            Name = "get_weather",
            Description = "Get the weather for a location",
            Parameters = new Dictionary<string, JsonSchema> { ["location"] = new JsonSchema { Type = "string" } }
        };
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool> { tool },
            ToolChoice = "required"
        };

        await _client.ChatAsync(request);

        var logEntries = _server.LogEntries;
        var requestLog = logEntries.Should().ContainSingle().Subject;
        var body = requestLog.RequestMessage.Body?.ToString() ?? string.Empty;
        body.Should().Contain("\"tool_choice\":\"required\"");
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
    public async Task StreamAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":""Rate limit""}";
        var success = "{\"message\":{\"content\":\"Retry\"},\"done\":true}\n";
        StubRetryScenario("/api/chat", fail, success);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Retry");
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
        result.Embedding.Should().BeEquivalentTo(new float[] { 1f, 2f, 3f });
        result.Model.Should().Be("ollama");
    }

    [Fact]
    public async Task EmbeddingsAsync_Error_ThrowsLLMConnectException()
    {
        StubEmbeddings(@"{""error"":""Invalid input""}", HttpStatusCode.BadRequest);

        Func<Task> act = async () => await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Ollama");
        ex.Which.Message.Should().Contain("Invalid input");
    }
}