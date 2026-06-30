using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Diagnostics;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LLMConnect.Tests;

/// <summary>
/// Integration tests that simulate all four LLM providers using WireMock.
/// No real API keys or network calls are made.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LLMConnectClient _openAiClient;
    private readonly LLMConnectClient _anthropicClient;
    private readonly LLMConnectClient _googleClient;
    private readonly LLMConnectClient _ollamaClient;

    public IntegrationTests()
    {
        _server = WireMockServer.Start();
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var baseAddress = _server.Urls[0];

        _openAiClient = CreateClient(ProviderType.OpenAI, baseAddress, "/v1/chat/completions");
        _anthropicClient = CreateClient(ProviderType.Anthropic, baseAddress, "/v1/messages");
        _googleClient = CreateClient(ProviderType.Google, baseAddress, "/v1beta/models/gemini-2.0-flash:generateContent");
        _ollamaClient = CreateClient(ProviderType.Ollama, baseAddress, "/api/chat");
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        _loggerFactory.Dispose();
    }

    private LLMConnectClient CreateClient(ProviderType provider, string baseAddress, string path)
    {
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = provider != ProviderType.Ollama ? "test-key" : null,
            Endpoint = baseAddress + path, // Full URL including path
            LoggerFactory = _loggerFactory,
            MaxRetries = 2,
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Use the constructor that creates its own HttpClient with retry handler
        return new LLMConnectClient(options);
    }

    // ---------- Helper Methods to Stub Responses ----------

    #region OpenAI Stubs

    private void StubOpenAIChatResponse(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create()
                .WithPath("/v1/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));
    }

    private void StubOpenAIStreamResponse(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var streamBuilder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            streamBuilder.AppendLine($"data: {chunk}");
            streamBuilder.AppendLine();
        }
        streamBuilder.AppendLine("data: [DONE]");
        streamBuilder.AppendLine();

        _server
            .Given(Request.Create()
                .WithPath("/v1/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(streamBuilder.ToString()));
    }

    #endregion

    #region Anthropic Stubs

    private void StubAnthropicChatResponse(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create()
                .WithPath("/v1/messages")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));
    }

    private void StubAnthropicStreamResponse(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var streamBuilder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            streamBuilder.AppendLine("event: content_block_delta");
            streamBuilder.AppendLine($"data: {chunk}");
            streamBuilder.AppendLine();
        }
        streamBuilder.AppendLine("event: message_stop");
        streamBuilder.AppendLine("data: {}");
        streamBuilder.AppendLine();

        _server
            .Given(Request.Create()
                .WithPath("/v1/messages")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(streamBuilder.ToString()));
    }

    #endregion

    #region Google Stubs

    private void StubGoogleChatResponse(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create()
                .WithPath("/v1beta/models/gemini-2.0-flash:generateContent")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));
    }

    private void StubGoogleStreamResponse(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var streamBuilder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            streamBuilder.AppendLine($"data: {chunk}");
            streamBuilder.AppendLine();
        }

        _server
            .Given(Request.Create()
                .WithPath("/v1beta/models/gemini-2.0-flash:streamGenerateContent")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(streamBuilder.ToString()));
    }

    #endregion

    #region Ollama Stubs

    private void StubOllamaChatResponse(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create()
                .WithPath("/api/chat")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));
    }

    private void StubOllamaStreamResponse(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var streamBuilder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            streamBuilder.AppendLine(chunk);
        }

        _server
            .Given(Request.Create()
                .WithPath("/api/chat")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(streamBuilder.ToString()));
    }

    #endregion

    // ---------- Retry Scenario Helpers ----------

    /// <summary>
    /// Sets up a retry scenario where the first 'failCount' responses are failures,
    /// and the next response is a success.
    /// </summary>
    private void SetupRetryScenario(string path, string failResponse, string successResponse,
        HttpStatusCode failStatusCode = HttpStatusCode.TooManyRequests,
        int failCount = 2)
    {
        // We'll use WireMock's scenario feature to handle state.
        var scenario = "retry_scenario_" + Guid.NewGuid().ToString("N");

        // Step 0: initial state (first request)
        for (int i = 0; i < failCount; i++)
        {
            var nextState = (i < failCount - 1) ? $"{i + 1}" : "final"; // after last failure, go to final
            _server
                .Given(Request.Create()
                    .WithPath(path)
                    .UsingPost())
                .InScenario(scenario)
                .WhenStateIs(i == 0 ? null : $"{i}") // null for initial state
                .WillSetStateTo(nextState)
                .RespondWith(Response.Create()
                    .WithStatusCode(failStatusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(failResponse));
        }

        // Final successful response
        _server
            .Given(Request.Create()
                .WithPath(path)
                .UsingPost())
            .InScenario(scenario)
            .WhenStateIs("final")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(successResponse));
    }

    // ---------- Helper to create a chat request ----------

    private ChatRequest CreateChatRequest(string userMessage = "Hello")
    {
        return new ChatRequest
        {
            Messages = new List<Message> { new UserMessage(userMessage) }
        };
    }

    // ---------- OpenAI Tests ----------

    [Fact]
    public async Task OpenAIChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var responseJson = @"
        {
            ""id"": ""chatcmpl-123"",
            ""model"": ""gpt-3.5-turbo"",
            ""created"": 1677651234,
            ""choices"": [
                {
                    ""index"": 0,
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Hello, world!""
                    },
                    ""finish_reason"": ""stop""
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 5,
                ""total_tokens"": 15
            }
        }";
        StubOpenAIChatResponse(responseJson);

        var result = await _openAiClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello, world!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task OpenAIStreamAsync_ValidResponse_ReturnsChatChunks()
    {
        var chunks = new[]
        {
            @"{""choices"":[{""delta"":{""content"":""Hello""}}]}",
            @"{""choices"":[{""delta"":{""content"":"" world""}}]}"
        };
        StubOpenAIStreamResponse(chunks);

        var result = await _openAiClient.StreamAsync(CreateChatRequest("Say hello")).ToListAsync();

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
    }

    [Fact]
    public async Task OpenAIChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        StubOpenAIChatResponse(errorJson, HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _openAiClient.ChatAsync(CreateChatRequest());

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("OpenAI");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    // ---------- Anthropic Tests ----------

    [Fact]
    public async Task AnthropicChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var responseJson = @"
        {
            ""id"": ""msg_123"",
            ""model"": ""claude-3-5-sonnet-20241022"",
            ""stop_reason"": ""end_turn"",
            ""content"": [{ ""type"": ""text"", ""text"": ""Hello from Anthropic!"" }],
            ""usage"": { ""input_tokens"": 10, ""output_tokens"": 5 }
        }";
        StubAnthropicChatResponse(responseJson);

        var result = await _anthropicClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from Anthropic!");
        result.FinishReason.Should().Be("end_turn");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task AnthropicStreamAsync_ValidResponse_ReturnsChatChunks()
    {
        var chunks = new[]
        {
            @"{""delta"":{""text"":""Hello""}}",
            @"{""delta"":{""text"":"" from Anthropic!""}}"
        };
        StubAnthropicStreamResponse(chunks);

        var result = await _anthropicClient.StreamAsync(CreateChatRequest("Say hello")).ToListAsync();

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" from Anthropic!");
    }

    [Fact]
    public async Task AnthropicChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        StubAnthropicChatResponse(errorJson, HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _anthropicClient.ChatAsync(CreateChatRequest());

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Anthropic");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    // ---------- Google Tests ----------

    [Fact]
    public async Task GoogleChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var responseJson = @"
        {
            ""candidates"": [
                {
                    ""content"": {
                        ""parts"": [{ ""text"": ""Hello from Google!"" }]
                    },
                    ""finishReason"": ""STOP""
                }
            ],
            ""usageMetadata"": {
                ""promptTokenCount"": 10,
                ""candidatesTokenCount"": 5
            }
        }";
        StubGoogleChatResponse(responseJson);

        var result = await _googleClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from Google!");
        result.FinishReason.Should().Be("STOP");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task GoogleStreamAsync_ValidResponse_ReturnsChatChunks()
    {
        var chunks = new[]
        {
            @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}",
            @"{""candidates"":[{""content"":{""parts"":[{""text"":"" from Google!""}]}}]}",
            @"{""candidates"":[{""finishReason"":""STOP""}]}"
        };
        StubGoogleStreamResponse(chunks);

        var result = await _googleClient.StreamAsync(CreateChatRequest("Say hello")).ToListAsync();

        result.Should().HaveCount(3);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" from Google!");
        result[2].IsComplete.Should().BeTrue();
        result[2].FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task GoogleChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":{""message"":""Invalid API key""}}";
        StubGoogleChatResponse(errorJson, HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _googleClient.ChatAsync(CreateChatRequest());

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Google");
        exception.Which.Message.Should().Be("Invalid API key");
    }

    // ---------- Ollama Tests ----------

    [Fact]
    public async Task OllamaChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var responseJson = @"
        {
            ""model"": ""llama3.2"",
            ""message"": { ""role"": ""assistant"", ""content"": ""Hello from Ollama!"" },
            ""done"": true,
            ""done_reason"": ""stop"",
            ""eval_count"": 5,
            ""prompt_eval_count"": 10
        }";
        StubOllamaChatResponse(responseJson);

        var result = await _ollamaClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello from Ollama!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task OllamaStreamAsync_ValidResponse_ReturnsChatChunks()
    {
        var chunks = new[]
        {
            @"{""message"":{""content"":""Hello""},""done"":false}",
            @"{""message"":{""content"":"" from Ollama!""},""done"":false}",
            @"{""message"":{""content"":""""},""done"":true}"
        };
        StubOllamaStreamResponse(chunks);

        var result = await _ollamaClient.StreamAsync(CreateChatRequest("Say hello")).ToListAsync();

        result.Should().HaveCount(3);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" from Ollama!");
        result[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task OllamaChatAsync_ErrorResponse_ThrowsLLMConnectException()
    {
        var errorJson = @"{""error"":""Internal server error""}";
        StubOllamaChatResponse(errorJson, HttpStatusCode.InternalServerError);

        Func<Task> act = async () => await _ollamaClient.ChatAsync(CreateChatRequest());

        var exception = await act.Should().ThrowAsync<LLMConnectException>();
        exception.Which.Provider.Should().Be("Ollama");
        exception.Which.Message.Should().Contain("Internal server error");
    }

    // ---------- Retry Tests ----------

    [Fact]
    public async Task OpenAI_ChatAsync_TransientFailure_RetriesAndSucceeds()
    {
        var failResponse = @"{""error"":{""message"":""Rate limit exceeded""}}";
        var successResponse = @"
    {
        ""id"": ""chatcmpl-123"",
        ""model"": ""gpt-3.5-turbo"",
        ""choices"": [{ ""message"": { ""content"": ""OpenAI retry worked!"" } }],
        ""usage"": { ""prompt_tokens"": 5, ""completion_tokens"": 2 }
    }";

        var scenario = "openai_retry_scenario";

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .InScenario(scenario)
            .WillSetStateTo("failed_once")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Content-Type", "application/json")
                .WithBody(failResponse));

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .InScenario(scenario)
            .WhenStateIs("failed_once")
            .WillSetStateTo("failed_twice")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Content-Type", "application/json")
                .WithBody(failResponse));

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .InScenario(scenario)
            .WhenStateIs("failed_twice")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(successResponse));

        var result = await _openAiClient.ChatAsync(CreateChatRequest());
        result.Should().NotBeNull();
        result.Content.Should().Be("OpenAI retry worked!");
    }

    [Fact]
    public async Task Anthropic_ChatAsync_TransientFailure_RetriesAndSucceeds()
    {
        var failResponse = @"{""error"":{""message"":""Rate limit exceeded""}}";
        var successResponse = @"
        {
            ""id"": ""msg_123"",
            ""model"": ""claude-3-5-sonnet-20241022"",
            ""stop_reason"": ""end_turn"",
            ""content"": [{ ""type"": ""text"", ""text"": ""Anthropic retry worked!"" }],
            ""usage"": { ""input_tokens"": 5, ""output_tokens"": 2 }
        }";

        SetupRetryScenario("/v1/messages", failResponse, successResponse);

        var result = await _anthropicClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Anthropic retry worked!");
    }

    [Fact]
    public async Task Google_ChatAsync_TransientFailure_RetriesAndSucceeds()
    {
        var failResponse = @"{""error"":{""message"":""Rate limit exceeded""}}";
        var successResponse = @"
        {
            ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""Google retry worked!"" }] } }],
            ""usageMetadata"": { ""promptTokenCount"": 5, ""candidatesTokenCount"": 2 }
        }";

        SetupRetryScenario("/v1beta/models/gemini-2.0-flash:generateContent", failResponse, successResponse);

        var result = await _googleClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Google retry worked!");
    }

    [Fact]
    public async Task Ollama_ChatAsync_TransientFailure_RetriesAndSucceeds()
    {
        var failResponse = @"{""error"":""Internal server error""}";
        var successResponse = @"
        {
            ""model"": ""llama3.2"",
            ""message"": { ""role"": ""assistant"", ""content"": ""Ollama retry worked!"" },
            ""done"": true,
            ""done_reason"": ""stop"",
            ""eval_count"": 5,
            ""prompt_eval_count"": 2
        }";

        SetupRetryScenario("/api/chat", failResponse, successResponse);

        var result = await _ollamaClient.ChatAsync(CreateChatRequest());

        result.Should().NotBeNull();
        result.Content.Should().Be("Ollama retry worked!");
    }
}