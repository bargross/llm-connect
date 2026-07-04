[![NuGet Version](https://img.shields.io/nuget/v/LLMConnect)](https://www.nuget.org/packages/LLMConnect/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Build Status](https://img.shields.io/github/actions/workflow/status/bargross/llm-connect/dotnet.yml?branch=main)](https://github.com/bargross/llm-connect/actions)

# LLMConnect

A provider‑agnostic .NET client for Large Language Models. Write your chat, embedding, and tool-calling logic once and run it against OpenAI, Anthropic, Google Gemini, or a local Ollama server through a single, consistent API.

---

## Table of contents

- [What is LLMConnect?](#what-is-llmconnect)
- [Features](#features)
- [Supported providers](#supported-providers)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Core concepts](#core-concepts)
  - [The client](#the-client)
  - [Messages](#messages)
  - [ChatRequest](#chatrequest)
  - [ChatResponse](#chatresponse)
  - [ChatChunk (streaming)](#chatchunk-streaming)
  - [EmbeddingRequest](#embeddingrequest)
  - [EmbeddingResponse](#embeddingresponse)
  - [Tool and JsonSchema](#tool-and-jsonschema)
  - [ToolCall](#toolcall)
  - [Usage](#usage)
- [Configuration](#configuration)
  - [Unified options: LLMConnectClientOptions](#unified-options-llmconnectclientoptions)
  - [Split options: LLMConnectGeneralOptions and LLMConnectEndpointOptions](#split-options-llmconnectgeneraloptions-and-llmconnectendpointoptions)
  - [Full options reference](#full-options-reference)
  - [Choosing a constructor](#choosing-a-constructor)
  - [Provider‑specific notes](#provider-specific-notes)
- [Embeddings](#embeddings)
  - [Basic usage](#basic-usage)
  - [Provider support matrix](#provider-support-matrix)
  - [Per-provider embedding options](#per-provider-embedding-options)
- [Tool calling](#tool-calling)
  - [Defining tools](#defining-tools)
  - [Sending tools in a request](#sending-tools-in-a-request)
  - [Handling tool call responses](#handling-tool-call-responses)
  - [Returning tool results](#returning-tool-results)
  - [Tool choice](#tool-choice)
  - [Provider support matrix](#tool-calling-provider-support-matrix)
  - [Provider-specific notes](#tool-calling-provider-specific-notes)
- [Custom endpoints and deserialization](#custom-endpoints-and-deserialization)
  - [Custom chat deserialization](#custom-chat-deserialization)
  - [Custom embedding deserialization](#custom-embedding-deserialization)
  - [Custom streaming readers and parsers](#custom-streaming-readers-and-parsers)
- [Dependency injection](#dependency-injection)
- [Retry behavior](#retry-behavior)
- [Streaming](#streaming)
- [Error handling](#error-handling)
- [Known limitations](#known-limitations)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## What is LLMConnect?

LLMConnect is a unified client library for .NET that abstracts away the differences between multiple LLM providers. It gives you one interface — `ILLMConnectClient` — for chat completions, streaming, vector embeddings, and tool/function calling, regardless of which provider sits behind it.

Stop learning a new SDK every time you want to switch providers or add a new capability. Write your application logic once against `ChatRequest`/`ChatResponse`/`EmbeddingRequest`/`EmbeddingResponse`, and change providers with a single configuration value.

---

## Features

- Provider‑agnostic core: one request/response model shape for OpenAI, Anthropic, Google Gemini, and Ollama
- Non‑streaming (`ChatAsync`) and streaming (`StreamAsync`) chat completions
- **Vector embeddings** (`GetEmbeddingAsync`) for OpenAI, Google, and Ollama
- **Tool/function calling** across all four providers — define tools once, use them with any provider
- **Split configuration** via `LLMConnectGeneralOptions` + `LLMConnectEndpointOptions` for fine-grained control, or a single `LLMConnectClientOptions` for simple setups
- **Custom response deserialization** for non-standard or proprietary endpoints — supply your own delegate for chat and/or embedding responses
- **Custom streaming readers and parsers** for non-standard streaming protocols on custom endpoints
- Built‑in retry with exponential backoff and jitter, backed by [Polly](https://github.com/App-vNext/Polly)
- Dependency Injection support via `Microsoft.Extensions.DependencyInjection`, with two `AddLLMConnect` overloads matching the unified/split options model
- Per‑instance default model, configurable timeout and retry count
- Strongly typed message roles (`SystemMessage`, `UserMessage`, `AssistantMessage`, `ToolMessage`)
- Optional structured logging via `Microsoft.Extensions.Logging`
- Full async/await and `IAsyncEnumerable` support for streaming
- Targets .NET 10

---

## Supported providers

### Chat completions

| Provider | Non‑Streaming | Streaming | Auth |
| :--- | :---: | :---: | :--- |
| OpenAI | ✅ | ✅ | `Authorization: Bearer <key>` |
| Anthropic | ✅ | ✅ | `x-api-key` header |
| Google Gemini | ✅ | ✅ | `x-goog-api-key` header |
| Ollama (local) | ✅ | ✅ | none |

### Embeddings

| Provider | Supported | Notes |
| :--- | :---: | :--- |
| OpenAI | ✅ | Full support: model, dimensions, encoding format |
| Google Gemini | ✅ | Full support: model, task type, title, role |
| Ollama (local) | ✅ | Full support: model, extra options pass-through |
| Anthropic | ❌ | Not supported by the Anthropic API |

### Tool calling

| Provider | Supported | Notes |
| :--- | :---: | :--- |
| OpenAI | ✅ | Full support including `tool_choice` and parallel tool calls |
| Anthropic | ✅ | Full support via `tool_use` content blocks |
| Google Gemini | ✅ | Full support via `functionDeclarations` and `functionCall` parts |
| Ollama (local) | ✅ | Supported on compatible models (e.g. `llama3.1`, `mistral-nemo`) |

---

## Installation

```bash
dotnet add package LLMConnect
```

Or via the NuGet Package Manager Console:

```powershell
Install-Package LLMConnect
```

---

## Quick start

### Chat

```csharp
using LLMConnect;
using LLMConnect.Models;
using LLMConnect.Settings;

var options = new LLMConnectClientOptions
{
    Provider     = ProviderType.OpenAI,
    ApiKey       = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    DefaultModel = "gpt-4o-mini"
};

using var client = new LLMConnectClient(options);

var request = new ChatRequest
{
    Messages =
    [
        new SystemMessage("You are a concise, helpful assistant."),
        new UserMessage("What's the capital of Romania?")
    ]
};

var response = await client.ChatAsync(request);
Console.WriteLine(response?.Content);
```

### Embeddings

```csharp
var embeddingRequest = new EmbeddingRequest
{
    Text  = "The quick brown fox jumps over the lazy dog.",
    Model = "text-embedding-3-small"
};

var embeddingResponse = await client.GetEmbeddingAsync(embeddingRequest);
float[] vector = embeddingResponse!.Embedding;
Console.WriteLine($"Embedding dimensions: {vector.Length}");
```

### Streaming

```csharp
await foreach (var chunk in client.StreamAsync(request))
{
    Console.Write(chunk.Content);

    if (chunk.IsComplete)
        Console.WriteLine($"\n[finished: {chunk.FinishReason}]");
}
```

### Tool calling

```csharp
var tools = new List<Tool>
{
    new Tool
    {
        Name        = "get_weather",
        Description = "Returns the current weather for a given city.",
        Parameters  = new Dictionary<string, JsonSchema>
        {
            ["city"] = new JsonSchema { Type = "string", Description = "The city name." }
        },
        Required = ["city"]
    }
};

var request = new ChatRequest
{
    Messages = [new UserMessage("What's the weather like in Bucharest?")],
    Tools    = tools
};

var response = await client.ChatAsync(request);

if (response?.ToolCalls?.Count > 0)
{
    var call = response.ToolCalls[0];
    Console.WriteLine($"Tool: {call.Name}");
    Console.WriteLine($"Args: {string.Join(", ", call.Arguments.Select(kv => $"{kv.Key}={kv.Value}"))}");
}
```

Switching providers is a configuration change, not a code change:

```csharp
var options = new LLMConnectClientOptions
{
    Provider     = ProviderType.Anthropic,
    ApiKey       = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!,
    DefaultModel = "claude-3-5-sonnet-20241022"
};
```

---

## Core concepts

### The client

The entry point is `ILLMConnectClient`, implemented by `LLMConnectClient`. It exposes three methods:

```csharp
Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
Task<EmbeddingResponse?> GetEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
```

`LLMConnectClient` implements `IDisposable`. If the client created its own `HttpClient`, disposing the client disposes it too. If you supplied your own `HttpClient` or `IHttpClientFactory`, you remain the owner.

### Messages

`ChatRequest.Messages` is a `List<Message>`. `Message` is abstract; construct one of the concrete role types:

```csharp
new SystemMessage("You are a helpful assistant.");
new UserMessage("Hello!");
new AssistantMessage("Hi, how can I help?");
new ToolMessage(toolCallId: "call_123", content: "{\"temperature\": 22}");
```

Each maps to the corresponding `MessageRole` (`System`, `User`, `Assistant`, `Tool`) and is translated into the wire format each provider expects.

### ChatRequest

```csharp
public class ChatRequest
{
    public List<Message> Messages { get; set; } = new();
    public string? SystemPrompt { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 0.9f;
    public int MaxTokens { get; set; } = 1024;
    public string? Model { get; set; }
    public List<string>? StopSequences { get; set; }
    public float? FrequencyPenalty { get; set; }        // OpenAI only
    public float? PresencePenalty { get; set; }         // OpenAI only
    public string? ResponseFormat { get; set; }         // "text" or "json_object"
    public int? Seed { get; set; }                      // OpenAI only
    public string? User { get; set; }
    public List<Tool>? Tools { get; set; }              // Tool/function calling
    public string? ToolChoice { get; set; }             // "auto", "required", "none", or tool name
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
```

Notes:

- `Model` overrides `DefaultModel` for a single request.
- Not every provider supports every field; unsupported fields are silently ignored.
- `ExtraParameters` is serialized as additional top-level JSON properties (`[JsonExtensionData]`), letting you pass provider-specific options without waiting for a library update.

### ChatResponse

```csharp
public class ChatResponse
{
    public string? Content { get; set; }
    public string? FinishReason { get; set; }
    public Usage Usage { get; set; } = new();
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }  // Populated when the model calls a tool
}
```

`ToolCalls` is non-null and non-empty when the model has chosen to invoke one or more tools instead of (or in addition to) generating text. Check this before assuming `Content` contains the final answer.

### ChatChunk (streaming)

```csharp
public class ChatChunk
{
    public string? Content { get; set; }
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
}
```

`IsComplete` is `true` on the final chunk. `FinishReason` is only populated on that chunk.

### EmbeddingRequest

```csharp
public class EmbeddingRequest
{
    public string? Text { get; set; }             // Required: the text to embed
    public string? Model { get; set; }            // Overrides DefaultModel for this request
    public string? User { get; set; }             // OpenAI: optional abuse-monitoring identifier
    public string? EncodingFormat { get; set; }   // OpenAI: "float" (default) or "base64"
    public int? Dimensions { get; set; }          // OpenAI: optional output dimensionality
    public string? TaskType { get; set; }         // Google: e.g. "RETRIEVAL_DOCUMENT", "RETRIEVAL_QUERY"
    public string? Title { get; set; }            // Google: optional title for document embeddings
    public string? Role { get; set; }             // Google: "user" or "model"
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
```

`Text` must be non-null and non-whitespace; validation throws before the request is sent. All other fields are optional and provider-specific.

### EmbeddingResponse

```csharp
public class EmbeddingResponse
{
    public float[] Embedding { get; set; }     // The embedding vector
    public string? Model { get; set; }         // Model that produced the embedding
    public EmbeddingUsage? Usage { get; set; } // Token usage, where available (OpenAI only)
    public DateTime CreatedAt { get; set; }
}
```

### Tool and JsonSchema

`Tool` is the provider-agnostic definition of a function the model can call. `JsonSchema` describes the shape of the tool's parameters.

```csharp
public class Tool
{
    public string Name { get; set; }                         // Function identifier
    public string Description { get; set; }                  // Explains to the model what the tool does
    public Dictionary<string, JsonSchema> Parameters { get; set; } = new(); // Parameter definitions
    public List<string> Required { get; set; } = new();     // Names of required parameters
}

public class JsonSchema
{
    public string Type { get; set; } = "string";            // "string", "number", "boolean", "object", "array"
    public string? Description { get; set; }
    public JsonSchema? Items { get; set; }                  // For array types: schema of each element
    public Dictionary<string, JsonSchema>? Properties { get; set; } // For object types: nested properties
    public List<object>? Enum { get; set; }                 // Restrict to a fixed set of values
    public Dictionary<string, object>? Extra { get; set; }  // Any additional JSON Schema keywords
}
```

### ToolCall

`ToolCall` appears on `ChatResponse.ToolCalls` when the model chose to invoke a tool:

```csharp
public class ToolCall
{
    public string Id { get; set; }                          // Unique call ID (used in ToolMessage)
    public string Name { get; set; }                        // The tool name the model selected
    public Dictionary<string, object> Arguments { get; set; } // Parsed arguments
}
```

### Usage

```csharp
public class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens; // computed
}
```

---

## Configuration

LLMConnect offers two configuration models. Choose whichever fits your application architecture.

### Unified options: LLMConnectClientOptions

All settings in one object. Ideal for most applications.

```csharp
var options = new LLMConnectClientOptions
{
    Provider     = ProviderType.OpenAI,
    ApiKey       = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    DefaultModel = "gpt-4o-mini",
    Timeout      = TimeSpan.FromSeconds(90),
    MaxRetries   = 3
};

using var client = new LLMConnectClient(options);
```

### Split options: LLMConnectGeneralOptions and LLMConnectEndpointOptions

For scenarios where you want to separate identity/auth from endpoint/deserialization concerns — for example, when the endpoint or custom deserializer is determined at runtime, or when you want to register them independently in DI.

```csharp
var generalOpts = new LLMConnectGeneralOptions
{
    Provider     = ProviderType.OpenAI,
    ApiKey       = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    DefaultModel = "gpt-4o-mini",
    MaxRetries   = 3
};

var endpointOpts = new LLMConnectEndpointOptions
{
    Endpoint = "https://my-azure-openai-proxy.example.com/openai/deployments/my-deployment/",
    ChatResponseDeserializer = async (json, ct) =>
    {
        var doc = JsonDocument.Parse(json);
        return new ChatResponse { Content = doc.RootElement.GetProperty("output").GetString() };
    }
};

using var client = new LLMConnectClient(generalOpts, endpointOpts);
```

### Full options reference

#### LLMConnectGeneralOptions / LLMConnectClientOptions (shared fields)

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Provider` | `ProviderType` | `OpenAI` | Target provider: `OpenAI`, `Anthropic`, `Google`, `Ollama`. |
| `ApiKey` | `string` | `""` | Provider API key. Not required for `Ollama`. |
| `DefaultModel` | `string?` | `null` | Model used when no model is specified on the request. Falls back to a per-provider default if also unset (see below). |
| `Timeout` | `TimeSpan` | `60s` | Per-request HTTP timeout. |
| `MaxRetries` | `int` | `3` | Maximum retry attempts on transient failures (`>= 0`; `0` disables retries). |
| `LoggerFactory` | `ILoggerFactory?` | `null` | If provided, LLMConnect emits structured logs (retries, errors, validation). |

**Per-provider model fallbacks** (used when no `DefaultModel` is set and no model is specified on the request):

| Provider | Fallback model |
| :--- | :--- |
| OpenAI | `gpt-3.5-turbo` |
| Anthropic | `claude-3-5-sonnet-20241022` |
| Google | `gemini-2.0-flash` |
| Ollama | `llama3.2` |

#### LLMConnectEndpointOptions

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Endpoint` | `string?` | `null` | Override the provider's default endpoint URL. Must be HTTPS for non-Ollama providers (except localhost). |
| `OllamaPort` | `int?` | `11434` | Port for a local Ollama server. Ignored if `Endpoint` is set. |
| `ChatResponseDeserializer` | `Func<string, CancellationToken, Task<ChatResponse?>>?` | `null` | Custom delegate to deserialize chat responses. Only invoked when `Endpoint` is set and this is non-null. |
| `EmbeddingResponseDeserializer` | `Func<string, CancellationToken, Task<EmbeddingResponse?>>?` | `null` | Custom delegate to deserialize embedding responses. Same conditions as above. |
| `CustomStreamEventReaderFactory` | `Func<IStreamEventReader>?` | `null` | Factory for a custom stream event reader. Active when paired with `CustomStreamChunkParserFactory` and a custom endpoint is set. |
| `CustomStreamChunkParserFactory` | `Func<IStreamChunkParser>?` | `null` | Factory for a custom chunk parser. Must be paired with `CustomStreamEventReaderFactory`. |
| `ExtraOptions` | `Dictionary<string, object>?` | `null` | Reserved for future provider-specific configuration. |

### Choosing a constructor

`LLMConnectClient` supports six constructors:

```csharp
// Unified options — library manages its own HttpClient
new LLMConnectClient(LLMConnectClientOptions options)

// Unified options — you supply the HttpClient (you own its lifetime and retry config)
new LLMConnectClient(LLMConnectClientOptions options, HttpClient httpClient)

// Unified options — you supply an IHttpClientFactory (recommended for ASP.NET Core)
new LLMConnectClient(LLMConnectClientOptions options, IHttpClientFactory httpClientFactory)

// Split options — library manages its own HttpClient
new LLMConnectClient(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions? endpointOpts)

// Split options — you supply the HttpClient
new LLMConnectClient(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, HttpClient httpClient)

// Split options — you supply an IHttpClientFactory
new LLMConnectClient(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, IHttpClientFactory httpClientFactory)
```

> **Important:** Retry behavior differs by constructor. Constructors that do not accept an external `HttpClient` automatically attach LLMConnect's retry handler. If you supply your own `HttpClient`, configure retry yourself — LLMConnect will log a warning and will not modify the client it doesn't own.

### Provider‑specific notes

**OpenAI** — authentication via `Authorization: Bearer <key>`. Streaming ends with a `data: [DONE]` sentinel. Supports all `ChatRequest` parameters including `Seed`, `FrequencyPenalty`, `PresencePenalty`, and `ResponseFormat`. Full tool calling and embedding support.

**Anthropic** — authentication via `x-api-key` header. `anthropic-version: 2023-06-01` is set automatically. Uses named SSE events (`content_block_delta`, `message_stop`, etc.) rather than a `[DONE]` sentinel. Tool calls are returned as `tool_use` content blocks in the response. **Embeddings are not supported by the Anthropic API**; calling `GetEmbeddingAsync` with an Anthropic provider throws `NotSupportedException`.

**Google Gemini** — authentication via `x-goog-api-key` header. Streaming requests automatically receive `alt=sse`. The stream ends at connection close; `finishReason` on the final chunk (e.g. `STOP`, `MAX_TOKENS`, `SAFETY`) is surfaced via `ChatChunk.FinishReason`. Tool calls are returned as `functionCall` parts inside response candidates. Supports `TaskType`, `Title`, and `Role` for embeddings.

**Ollama** — targets `http://localhost:{port}/api/` (default port `11434`), no authentication required. Streaming uses NDJSON. Tool calling requires a compatible model (e.g. `llama3.1`, `mistral-nemo`). Embedding endpoint is `/api/embeddings`.

---

## Embeddings

### Basic usage

```csharp
var request = new EmbeddingRequest
{
    Text = "The quick brown fox jumps over the lazy dog."
};

var response = await client.GetEmbeddingAsync(request);

float[] vector  = response!.Embedding;
string? model   = response.Model;
int? tokenCount = response.Usage?.TotalTokens;
```

If `EmbeddingRequest.Model` is not set, `DefaultModel` from options is used. If that is also not set, a per-provider default applies (e.g. `text-embedding-3-small` for OpenAI).

### Provider support matrix

| Feature | OpenAI | Google | Ollama |
| :--- | :---: | :---: | :---: |
| Basic embedding | ✅ | ✅ | ✅ |
| Custom model | ✅ | ✅ | ✅ |
| Token usage in response | ✅ | ❌ | ❌ |
| Dimensions control | ✅ | ❌ | ❌ |
| Encoding format (`float`/`base64`) | ✅ | ❌ | ❌ |
| Task type | ❌ | ✅ | ❌ |
| Title (document embeddings) | ❌ | ✅ | ❌ |
| Role | ❌ | ✅ | ❌ |
| Extra options pass-through | ✅ | ✅ | ✅ |

### Per-provider embedding options

**OpenAI:**
```csharp
var request = new EmbeddingRequest
{
    Text           = "Hello, world!",
    Model          = "text-embedding-3-large",
    Dimensions     = 512,        // reduce output dimensions (v3 models only)
    EncodingFormat = "float",    // "float" (default) or "base64"
    User           = "user-abc"  // optional, for abuse monitoring
};
```

**Google:**
```csharp
var request = new EmbeddingRequest
{
    Text     = "Retrieval document text goes here.",
    Model    = "text-embedding-004",
    TaskType = "RETRIEVAL_DOCUMENT", // RETRIEVAL_DOCUMENT | RETRIEVAL_QUERY |
                                     // CLASSIFICATION | CLUSTERING | SEMANTIC_SIMILARITY
    Title    = "My Document Title",  // optional, improves retrieval quality
    Role     = "user"                // "user" or "model"
};
```

**Ollama:**
```csharp
var request = new EmbeddingRequest
{
    Text  = "Embed this locally.",
    Model = "nomic-embed-text",
    ExtraParameters = new Dictionary<string, object>
    {
        ["num_ctx"] = 2048  // any Ollama model option can be passed here
    }
};
```

---

## Tool calling

Tool calling (also known as function calling) lets the model request that your application execute a specific function and return the result, enabling the model to interact with external systems, APIs, or data sources.

### Defining tools

Each `Tool` has a name, a description the model uses to decide when to call it, and a `Parameters` dictionary mapping parameter names to their `JsonSchema` definitions.

```csharp
var weatherTool = new Tool
{
    Name        = "get_weather",
    Description = "Returns the current temperature and conditions for a given city.",
    Parameters  = new Dictionary<string, JsonSchema>
    {
        ["city"] = new JsonSchema
        {
            Type        = "string",
            Description = "The city name, e.g. 'Bucharest'."
        },
        ["unit"] = new JsonSchema
        {
            Type        = "string",
            Description = "Temperature unit.",
            Enum        = new List<object> { "celsius", "fahrenheit" }
        }
    },
    Required = ["city"]
};
```

`JsonSchema` supports nested types for complex parameters:

```csharp
// Object parameter
["address"] = new JsonSchema
{
    Type       = "object",
    Properties = new Dictionary<string, JsonSchema>
    {
        ["street"] = new JsonSchema { Type = "string" },
        ["city"]   = new JsonSchema { Type = "string" }
    }
}

// Array parameter
["tags"] = new JsonSchema
{
    Type  = "array",
    Items = new JsonSchema { Type = "string" }
}
```

### Sending tools in a request

Attach your tool list to `ChatRequest.Tools`:

```csharp
var request = new ChatRequest
{
    Messages = [new UserMessage("What's the weather like in Bucharest right now?")],
    Tools    = [weatherTool]
};

var response = await client.ChatAsync(request);
```

### Handling tool call responses

When the model decides to call a tool, `ChatResponse.ToolCalls` is populated and `Content` may be empty. Check `ToolCalls` first:

```csharp
if (response?.ToolCalls?.Count > 0)
{
    foreach (var call in response.ToolCalls)
    {
        Console.WriteLine($"Tool requested: {call.Name}");
        Console.WriteLine($"Call ID: {call.Id}");

        foreach (var (param, value) in call.Arguments)
            Console.WriteLine($"  {param} = {value}");
    }
}
else
{
    Console.WriteLine(response?.Content);
}
```

### Returning tool results

After executing the tool, return the result to the model by appending the assistant's tool-call message and a `ToolMessage` to the conversation, then sending a follow-up request:

```csharp
// 1. Model calls the tool
var response = await client.ChatAsync(request);
var toolCall = response!.ToolCalls![0];

// 2. Execute the tool in your application
var weatherResult = await GetWeatherAsync(toolCall.Arguments["city"].ToString()!);

// 3. Return the result to the model
var followUpRequest = new ChatRequest
{
    Messages =
    [
        new UserMessage("What's the weather like in Bucharest right now?"),
        new AssistantMessage(response.Content ?? string.Empty),
        new ToolMessage(toolCallId: toolCall.Id, content: weatherResult)
    ],
    Tools = [weatherTool]
};

var finalResponse = await client.ChatAsync(followUpRequest);
Console.WriteLine(finalResponse?.Content);
```

### Tool choice

`ChatRequest.ToolChoice` controls whether and how the model uses tools:

| Value | Behavior |
| :--- | :--- |
| `null` / not set | Provider default (usually `"auto"`) |
| `"auto"` | Model decides whether to call a tool |
| `"required"` | Model must call at least one tool |
| `"none"` | Model must not call any tools |
| A tool name (e.g. `"get_weather"`) | Model must call that specific tool |

```csharp
var request = new ChatRequest
{
    Messages   = [new UserMessage("Get the weather for Bucharest.")],
    Tools      = [weatherTool],
    ToolChoice = "required"  // force the model to use a tool
};
```

### Tool calling provider support matrix

| Feature | OpenAI | Anthropic | Google | Ollama |
| :--- | :---: | :---: | :---: | :---: |
| Basic tool calling | ✅ | ✅ | ✅ | ✅ |
| Multiple tools per request | ✅ | ✅ | ✅ | ✅ |
| `"auto"` tool choice | ✅ | ✅ | ✅ | ✅ |
| `"required"` tool choice | ✅ | ✅ | ✅ (mapped to `ANY`) | ✅ |
| `"none"` tool choice | ✅ | ✅ | ✅ | ✅ |
| Specific tool by name | ✅ | ✅ | ✅ | ✅ |
| Parallel tool calls | ✅ | ✅ | ❌ | depends on model |

### Tool calling provider-specific notes

**OpenAI** — tool calls are returned in `message.tool_calls` on the response. `ToolChoice` of a specific tool name is sent as `{ "type": "function", "function": { "name": "..." } }`. `ToolCall.Id` is OpenAI's call ID and must be echoed back in the `ToolMessage`.

**Anthropic** — tool definitions are sent as top-level `tools` on the request. Tool calls are returned as `tool_use` content blocks; `ToolCall.Id` is Anthropic's `tool_use` ID and must be echoed back. `ToolChoice` is forwarded as-is (`"auto"`, `"required"`, or a tool name).

**Google** — tool definitions are grouped into a single `tools[0].functionDeclarations` array on the request. `ToolChoice` of `"required"` maps to `ANY` in `functionCallingConfig.mode`; a specific tool name maps to `ANY` with `allowedFunctionNames`. `ToolCall.Id` is set to the function name (Google does not issue separate call IDs).

**Ollama** — tool support depends on the loaded model. Models known to work well include `llama3.1`, `mistral-nemo`, and `command-r`. `ToolCall.Id` is set to the function name (Ollama does not issue separate call IDs). Validate your model supports tool calling before deploying.

---

## Custom endpoints and deserialization

LLMConnect supports routing requests to non-default endpoints — Azure OpenAI deployments, reverse proxies, self-hosted inference servers — without losing the convenience of the unified client. You can optionally supply custom deserialization when the endpoint returns a different response shape.

### Custom chat deserialization

When `Endpoint` is set and a `ChatResponseDeserializer` delegate is provided, the delegate is called with the raw JSON response body. If no delegate is provided, standard provider-specific deserialization is used even with a custom endpoint.

```csharp
var endpointOpts = new LLMConnectEndpointOptions
{
    Endpoint = "https://my-proxy.example.com/v1/chat",
    ChatResponseDeserializer = async (json, cancellationToken) =>
    {
        using var doc = await JsonDocument.ParseAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(json)), cancellationToken: cancellationToken);

        return new ChatResponse
        {
            Content      = doc.RootElement.GetProperty("result").GetString(),
            FinishReason = "stop",
            CreatedAt    = DateTime.UtcNow
        };
    }
};
```

### Custom embedding deserialization

Same pattern for embeddings:

```csharp
var endpointOpts = new LLMConnectEndpointOptions
{
    Endpoint = "https://my-embed-server.example.com/embed",
    EmbeddingResponseDeserializer = async (json, cancellationToken) =>
    {
        using var doc = await JsonDocument.ParseAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(json)), cancellationToken: cancellationToken);

        var values = doc.RootElement
            .GetProperty("vectors")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return new EmbeddingResponse { Embedding = values, CreatedAt = DateTime.UtcNow };
    }
};
```

### Custom streaming readers and parsers

For custom endpoints that stream in a non-standard format, supply both a reader and a parser together. If either is absent, the built-in reader/parser for the configured provider is used.

```csharp
var endpointOpts = new LLMConnectEndpointOptions
{
    Endpoint                   = "https://my-streaming-api.example.com/stream",
    CustomStreamEventReaderFactory = () => new MyCustomStreamReader(),
    CustomStreamChunkParserFactory = () => new MyCustomChunkParser()
};
```

`IStreamEventReader` reads raw events from the stream:

```csharp
public interface IStreamEventReader
{
    IAsyncEnumerable<StreamedEvent> ReadEventsAsync(Stream stream, CancellationToken cancellationToken = default);
}
```

`IStreamChunkParser` turns a `StreamedEvent` into a `ChatChunk`:

```csharp
public interface IStreamChunkParser
{
    ChatChunk? Parse(StreamedEvent evt);
}
```

`StreamedEvent` carries the raw SSE event name and data payload:

```csharp
public readonly record struct StreamedEvent(string? EventName, string Data);
```

---

## Dependency injection

LLMConnect integrates with `Microsoft.Extensions.DependencyInjection` via `AddLLMConnect`. Two overloads are available matching the unified and split configuration models.

### Unified options

```csharp
using LLMConnect.Configuration;

builder.Services.AddLLMConnect(options =>
{
    options.Provider     = ProviderType.OpenAI;
    options.ApiKey       = builder.Configuration["OpenAI:ApiKey"]!;
    options.DefaultModel = "gpt-4o-mini";
    options.MaxRetries   = 3;
});
```

### Split options

```csharp
builder.Services.AddLLMConnect(
    configureGeneral: general =>
    {
        general.Provider     = ProviderType.OpenAI;
        general.ApiKey       = builder.Configuration["OpenAI:ApiKey"]!;
        general.DefaultModel = "gpt-4o-mini";
    },
    configureEndpoint: endpoint =>
    {
        endpoint.Endpoint = builder.Configuration["OpenAI:CustomEndpoint"];
    }
);
```

Both overloads register:

- A named `HttpClient` (`"LLMConnect"`) with connection pooling (`SocketsHttpHandler.PooledConnectionLifetime = 5 minutes`) and LLMConnect's retry handler wired to `MaxRetries`.
- `ILLMConnectClient` as a singleton.

Inject and use it like any other service:

```csharp
public class MyService(ILLMConnectClient client)
{
    public Task<ChatResponse?> AskAsync(string question) =>
        client.ChatAsync(new ChatRequest { Messages = [new UserMessage(question)] });

    public Task<EmbeddingResponse?> EmbedAsync(string text) =>
        client.GetEmbeddingAsync(new EmbeddingRequest { Text = text });

    public Task<ChatResponse?> AskWithToolsAsync(string question, List<Tool> tools) =>
        client.ChatAsync(new ChatRequest
        {
            Messages = [new UserMessage(question)],
            Tools    = tools
        });
}
```

---

## Retry behavior

LLMConnect retries requests that fail with:

- HTTP `429 Too Many Requests`
- HTTP `5xx` server errors
- `HttpRequestException` (network-level failures, e.g. connection reset, DNS failure)

Retries use exponential backoff with jitter (powered by Polly's `ResiliencePipeline`), up to `MaxRetries` attempts (default `3`; `0` disables retries). Each retry is logged at `Warning` level when a `LoggerFactory` is configured, with the attempt number, delay, and reason.

**Things to be aware of:**

- Retries are not currently aware of the `Retry-After` header that some providers return on `429` responses. Backoff is always computed locally.
- A retried request that timed out may have already been processed server-side. LLMConnect does not currently send idempotency keys, so a timeout-triggered retry can result in the provider billing for more than one completion for a single logical call.
- Retry is only automatically attached when LLMConnect creates its own `HttpClient`. If you supply your own, configure retry yourself.

---

## Streaming

`StreamAsync` returns `IAsyncEnumerable<ChatChunk>` and is consumed with `await foreach`:

```csharp
var sb = new StringBuilder();

await foreach (var chunk in client.StreamAsync(request, cancellationToken))
{
    sb.Append(chunk.Content);

    if (chunk.IsComplete)
        Console.WriteLine($"\nDone. Reason: {chunk.FinishReason}");
}
```

Cancellation is fully supported — pass a `CancellationToken` and the read loop stops cleanly.

Internally, streaming is implemented as two small, testable layers:

- An **event reader** (`IStreamEventReader`) that handles the wire protocol (SSE for OpenAI/Anthropic/Google, NDJSON for Ollama) and yields raw `StreamedEvent` values.
- A **chunk parser** (`IStreamChunkParser`), one per provider, that maps a raw event to a `ChatChunk` or discards non-content events.

Because the protocol loop exists once per protocol rather than once per provider, bugs in stream reading cannot silently diverge across providers.

---

## Error handling

All provider errors are surfaced as `LLMConnectException`:

```csharp
public class LLMConnectException : Exception
{
    public string? Provider { get; set; }
}
```

```csharp
try
{
    var response = await client.ChatAsync(request);
}
catch (LLMConnectException ex)
{
    Console.WriteLine($"[{ex.Provider}] {ex.Message}");
}
catch (NotSupportedException ex)
{
    // Thrown by GetEmbeddingAsync when using the Anthropic provider
    Console.WriteLine(ex.Message);
}
```

LLMConnect extracts a human-readable message from the provider's JSON error body (OpenAI/Anthropic: `error.message`; Google: `error.message`; top-level `message` as a fallback). If the response body is not valid JSON (e.g. an HTML error page from a proxy or an empty 502 body), LLMConnect falls back to the raw HTTP status code and body text rather than throwing an unrelated `JsonException`.

---

## Known limitations

- **No batch embedding support.** `EmbeddingRequest.Text` accepts a single string; sending multiple texts in one API call is not currently supported.
- **No embeddings for Anthropic.** The Anthropic API does not offer an embeddings endpoint; `GetEmbeddingAsync` throws `NotSupportedException` when the Anthropic provider is configured.
- **`Retry-After` not honored** on `429` responses — backoff is always computed locally.
- **Streaming finish-reason fidelity varies by provider** — see the provider-specific notes for what each provider signals on stream completion.
- **Ollama tool calling requires a compatible model** — not all Ollama models support tool calling; check the model's documentation before use.

---

## Roadmap

### Now
- Chat completions (non-streaming and streaming) across OpenAI, Anthropic, Google, and Ollama
- Vector embeddings across OpenAI, Google, and Ollama
- Tool/function calling across all four providers
- Split configuration (`LLMConnectGeneralOptions` + `LLMConnectEndpointOptions`)
- Custom response deserialization and custom streaming for non-standard endpoints
- Retry with backoff and jitter, DI support

### Next
- Honor `Retry-After` on rate-limit responses
- Batch embedding support

### Later
- `Microsoft.Extensions.AI` integration
- Additional providers (e.g. Mistral, Cohere)

---

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a pull request, and include tests for new behavior — the project has an xUnit test suite (`LLMConnect.Tests`) covering providers, streaming, retry behavior, and configuration validation, including WireMock-based integration tests that simulate all four providers without making real network calls.

```bash
git clone https://github.com/bargross/llm-connect.git
cd llm-connect/LLMConnect
dotnet test
```

---

## License

This project is licensed under the Apache License, Version 2.0. See the [LICENSE](./LICENSE) file for details.