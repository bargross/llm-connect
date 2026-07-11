[![NuGet Version](https://img.shields.io/nuget/v/LLMConnect)](https://www.nuget.org/packages/LLMConnect/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Build Status](https://img.shields.io/github/actions/workflow/status/bargross/llm-connect/dotnet.yml?branch=main)](https://github.com/bargross/llm-connect/actions)
[![Changelog](https://img.shields.io/badge/Changelog-view-blue)](./CHANGELOG.md)

# LLMConnect

A provider-agnostic .NET client for Large Language Models. Write your chat, embedding, and tool-calling logic once and run it against OpenAI, Azure OpenAI, Anthropic, Google Gemini, or a local Ollama server through a single, consistent API.

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
  - [ToolCallDelta (streaming tool calls)](#toolcalldelta-streaming-tool-calls)
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
  - [Provider-specific notes](#provider-specific-notes)
- [Azure OpenAI](#azure-openai)
- [Embeddings](#embeddings)
  - [Basic usage](#basic-usage)
  - [Provider support matrix](#embeddings-provider-support-matrix)
  - [Per-provider embedding options](#per-provider-embedding-options)
- [Tool calling](#tool-calling)
  - [Defining tools](#defining-tools)
  - [Sending tools in a request](#sending-tools-in-a-request)
  - [Handling tool call responses](#handling-tool-call-responses)
  - [Returning tool results](#returning-tool-results)
  - [Tool choice](#tool-choice)
  - [Provider support matrix](#tool-calling-provider-support-matrix)
  - [Provider-specific notes](#tool-calling-provider-specific-notes)
- [Streaming tool calls](#streaming-tool-calls)
- [Dependency injection](#dependency-injection)
- [Retry behavior](#retry-behavior)
- [Error handling](#error-handling)
- [Known limitations](#known-limitations)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## What is LLMConnect?

LLMConnect is a unified client library for .NET that abstracts away the differences between multiple LLM providers. It gives you one interface — `ILLMConnectClient` — for chat completions, streaming, vector embeddings, and tool/function calling, regardless of which provider sits behind it.

Stop learning a new SDK every time you want to switch providers or add a capability. Write your application logic once against `ChatRequest`/`ChatResponse`/`EmbeddingRequest`/`EmbeddingResponse`, and change providers with a single configuration value.

---

## Features

- Provider-agnostic core — one request/response model for OpenAI, Azure OpenAI, Anthropic, Google Gemini, and Ollama
- Non-streaming (`ChatAsync`) and streaming (`StreamAsync`) chat completions
- **Vector embeddings** (`GetEmbeddingAsync`) for OpenAI, Azure OpenAI, Google, and Ollama
- **Tool/function calling** across all five providers, including streaming tool call deltas
- **Azure OpenAI** as a first-class provider — base URL constructed internally from named options, same wire format as OpenAI
- **Split configuration** via `LLMConnectGeneralOptions` + `LLMConnectEndpointOptions`, or a single `LLMConnectClientOptions` for simple setups
- Built-in retry with exponential backoff and jitter, backed by [Polly](https://github.com/App-vNext/Polly)
- Dependency Injection support via `Microsoft.Extensions.DependencyInjection`
- Strongly typed message roles (`SystemMessage`, `UserMessage`, `AssistantMessage`, `ToolMessage`)
- Optional structured logging via `Microsoft.Extensions.Logging`
- Full async/await and `IAsyncEnumerable` support for streaming
- Targets .NET 10

---

## Supported providers

### Chat completions

| Provider | Non-Streaming | Streaming | Auth |
| :--- | :---: | :---: | :--- |
| OpenAI | ✅ | ✅ | `Authorization: Bearer <key>` |
| Azure OpenAI | ✅ | ✅ | `api-key` header |
| Anthropic | ✅ | ✅ | `x-api-key` header |
| Google Gemini | ✅ | ✅ | `x-goog-api-key` header |
| Ollama (local) | ✅ | ✅ | none |

### Embeddings

| Provider | Supported | Notes |
| :--- | :---: | :--- |
| OpenAI | ✅ | Model, dimensions, encoding format |
| Azure OpenAI | ✅ | Same as OpenAI, routed through Azure deployment |
| Google Gemini | ✅ | Model, task type, title, role |
| Ollama (local) | ✅ | Model, extra options pass-through |
| Anthropic | ❌ | Not supported by the Anthropic API |

### Tool calling

| Provider | Supported | Notes |
| :--- | :---: | :--- |
| OpenAI | ✅ | Full support including parallel tool calls |
| Azure OpenAI | ✅ | Identical to OpenAI |
| Anthropic | ✅ | Via `tool_use` content blocks |
| Google Gemini | ✅ | Via `functionDeclarations` / `functionCall` |
| Ollama (local) | ✅ | Requires a compatible model |

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
        new UserMessage("What is the capital of Romania?")
    ]
};

var response = await client.ChatAsync(request);
Console.WriteLine(response?.Content);
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

### Embeddings

```csharp
var embeddingRequest = new EmbeddingRequest
{
    Text  = "The quick brown fox jumps over the lazy dog.",
    Model = "text-embedding-3-small"
};

var embeddingResponse = await client.GetEmbeddingAsync(embeddingRequest);
Console.WriteLine($"Dimensions: {embeddingResponse!.Embedding.Length}");
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
    Messages = [new UserMessage("What is the weather like in Bucharest?")],
    Tools    = tools
};

var response = await client.ChatAsync(request);

if (response?.ToolCalls?.Count > 0)
{
    var call = response.ToolCalls[0];
    Console.WriteLine($"Tool: {call.Name}, Args: {string.Join(", ", call.Arguments.Select(kv => $"{kv.Key}={kv.Value}"))}");
}
```

Switching providers is a one-line change:

```csharp
options.Provider = ProviderType.Anthropic;
options.ApiKey   = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
```

---

## Core concepts

### The client

The entry point is `ILLMConnectClient`, implemented by `LLMConnectClient`:

```csharp
Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
Task<EmbeddingResponse?> GetEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
```

`LLMConnectClient` implements `IDisposable`. If it created its own `HttpClient`, disposing the client disposes it too. If you supplied your own `HttpClient` or `IHttpClientFactory`, you remain the owner and LLMConnect will not dispose it.

### Messages

`ChatRequest.Messages` is a `List<Message>`. Construct one of the concrete role types:

```csharp
new SystemMessage("You are a helpful assistant.");
new UserMessage("Hello!");
new AssistantMessage("Hi, how can I help?");
new ToolMessage(toolCallId: "call_abc123", content: "{\"temperature\": 22}");
```

Each maps to the corresponding `MessageRole` (`System`, `User`, `Assistant`, `Tool`) and is serialized into the wire format each provider expects.

### ChatRequest

```csharp
public class ChatRequest
{
    public List<Message> Messages { get; set; }
    public string? SystemPrompt { get; set; }
    public float Temperature { get; set; }          // default 0.7
    public float TopP { get; set; }                 // default 0.9
    public int MaxTokens { get; set; }              // default 1024
    public string? Model { get; set; }              // overrides DefaultModel
    public List<string>? StopSequences { get; set; }
    public float? FrequencyPenalty { get; set; }    // OpenAI / Azure only
    public float? PresencePenalty { get; set; }     // OpenAI / Azure only
    public string? ResponseFormat { get; set; }     // "text" or "json_object"
    public int? Seed { get; set; }                  // OpenAI / Azure only
    public string? User { get; set; }
    public List<Tool>? Tools { get; set; }
    public string? ToolChoice { get; set; }         // "auto", "required", "none", or a tool name
    public Dictionary<string, object>? ExtraParameters { get; set; } // [JsonExtensionData]
}
```

`ExtraParameters` is serialized as additional top-level JSON properties, letting you pass provider-specific options without waiting for a library update.

### ChatResponse

```csharp
public class ChatResponse
{
    public string? Content { get; set; }
    public string? FinishReason { get; set; }
    public Usage Usage { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }  // non-null when the model calls a tool
}
```

Check `ToolCalls` before assuming `Content` contains the final answer — when the model calls a tool, `Content` may be empty or null.

### ChatChunk (streaming)

```csharp
public class ChatChunk
{
    public string? Content { get; set; }
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
    public List<ToolCallDelta>? ToolCalls { get; set; }  // populated during streamed tool calls
}
```

### ToolCallDelta (streaming tool calls)

During a stream, tool call arguments arrive as incremental JSON fragments across multiple chunks. Each chunk carries a `List<ToolCallDelta>` where `ArgumentsDelta` is the partial JSON for that chunk only. Accumulate `ArgumentsDelta` across all chunks with the same `Index` and deserialize the complete string when `IsComplete` is true.

```csharp
public class ToolCallDelta
{
    public int Index { get; set; }            // identifies which tool call this delta belongs to
    public string? Id { get; set; }           // present on the first delta for each call
    public string? Name { get; set; }         // present on the first delta for each call
    public string? ArgumentsDelta { get; set; } // partial JSON fragment for this chunk
}
```

Provider behaviour differences:

- **OpenAI / Azure**: `Id` and `Name` arrive on the first delta for each call index; subsequent deltas for the same index carry only `ArgumentsDelta`.
- **Anthropic**: `Id` and `Name` arrive on the first delta; argument fragments arrive as `input_json_delta` events. `FinishReason` is populated from `message_delta.stop_reason`.
- **Google**: each chunk contains a complete `functionCall` part (not incremental); `ArgumentsDelta` is the full serialized args JSON on each chunk.
- **Ollama**: tool calls arrive fully formed in a single chunk on supporting models.

### EmbeddingRequest

```csharp
public class EmbeddingRequest
{
    public string? Text { get; set; }             // required; must be non-null, non-whitespace
    public string? Model { get; set; }
    public string? User { get; set; }             // OpenAI / Azure: abuse monitoring
    public string? EncodingFormat { get; set; }   // OpenAI / Azure: "float" or "base64"
    public int? Dimensions { get; set; }          // OpenAI / Azure: output dimensionality
    public string? TaskType { get; set; }         // Google: e.g. "RETRIEVAL_DOCUMENT"
    public string? Title { get; set; }            // Google: document title
    public string? Role { get; set; }             // Google: "user" or "model"
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
```

### EmbeddingResponse

```csharp
public class EmbeddingResponse
{
    public float[] Embedding { get; set; }     // the embedding vector
    public string? Model { get; set; }
    public EmbeddingUsage? Usage { get; set; } // token usage; OpenAI/Azure only, null otherwise
    public DateTime CreatedAt { get; set; }
}
```

### Tool and JsonSchema

```csharp
public class Tool
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, JsonSchema> Parameters { get; set; }
    public List<string> Required { get; set; }
}

public class JsonSchema
{
    public string Type { get; set; }                           // "string", "number", "boolean", "object", "array"
    public string? Description { get; set; }
    public JsonSchema? Items { get; set; }                     // for array types
    public Dictionary<string, JsonSchema>? Properties { get; set; } // for object types
    public List<object>? Enum { get; set; }
    public Dictionary<string, object>? Extra { get; set; }    // additional JSON Schema keywords
}
```

### ToolCall

```csharp
public class ToolCall
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Arguments { get; set; } // fully deserialized
}
```

### Usage

```csharp
public class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
}
```

---

## Configuration

LLMConnect offers two configuration models.

### Unified options: LLMConnectClientOptions

All settings in one object. Suitable for most applications.

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

For Azure OpenAI, add the three Azure-specific fields:

```csharp
var options = new LLMConnectClientOptions
{
    Provider             = ProviderType.AzureOpenAI,
    ApiKey               = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!,
    AzureResourceName    = "my-resource",
    AzureDeploymentName  = "my-gpt4o-deployment",
    AzureApiVersion      = "2024-10-21"
};
```

### Split options: LLMConnectGeneralOptions and LLMConnectEndpointOptions

Separates identity/auth concerns from endpoint configuration. Useful when the endpoint options are determined at runtime, loaded from different configuration sources, or when you want to register each independently in DI.

```csharp
var generalOpts = new LLMConnectGeneralOptions
{
    Provider     = ProviderType.AzureOpenAI,
    ApiKey       = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!,
    DefaultModel = "gpt-4o-mini",
    MaxRetries   = 3
};

var endpointOpts = new LLMConnectEndpointOptions
{
    AzureResourceName   = "my-resource",
    AzureDeploymentName = "my-gpt4o-deployment",
    AzureApiVersion     = "2024-10-21"
};

using var client = new LLMConnectClient(generalOpts, endpointOpts);
```

### Full options reference

#### LLMConnectGeneralOptions / LLMConnectClientOptions (shared fields)

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Provider` | `ProviderType` | `OpenAI` | Target provider. |
| `ApiKey` | `string` | `""` | Provider API key. Not required for Ollama. |
| `DefaultModel` | `string?` | `null` | Model used when the request does not specify one. |
| `Timeout` | `TimeSpan` | `60s` | Per-request HTTP timeout. |
| `MaxRetries` | `int` | `3` | Maximum retry attempts (`0` disables retries). |
| `LoggerFactory` | `ILoggerFactory?` | `null` | If provided, LLMConnect emits structured logs. |

**Per-provider model fallbacks** (when no model is specified anywhere):

| Provider | Fallback |
| :--- | :--- |
| OpenAI / Azure OpenAI | `gpt-3.5-turbo` |
| Anthropic | `claude-3-5-sonnet-20241022` |
| Google | `gemini-2.0-flash` |
| Ollama | `llama3.2` |

#### LLMConnectClientOptions (additional unified-only fields)

These are duplicated on `LLMConnectEndpointOptions` for the split-options path:

| Property | Type | Description |
| :--- | :--- | :--- |
| `AzureResourceName` | `string?` | Azure resource subdomain (e.g. `my-resource` in `my-resource.openai.azure.com`). |
| `AzureDeploymentName` | `string?` | Azure model deployment name. |
| `AzureApiVersion` | `string?` | Azure API version query param (e.g. `2024-10-21`). |
| `OllamaPort` | `int?` | Ollama server port. Defaults to `11434`. |

#### LLMConnectEndpointOptions (split-options path)

| Property | Type | Description |
| :--- | :--- | :--- |
| `AzureResourceName` | `string?` | See above. |
| `AzureDeploymentName` | `string?` | See above. |
| `AzureApiVersion` | `string?` | See above. |
| `OllamaPort` | `int?` | See above. |
| `ExtraOptions` | `Dictionary<string, object>?` | Reserved for future use. |

### Choosing a constructor

```csharp
// Unified options — library manages its own HttpClient
new LLMConnectClient(LLMConnectClientOptions options)

// Unified options — you supply the HttpClient
new LLMConnectClient(LLMConnectClientOptions options, HttpClient httpClient)

// Unified options — you supply an IHttpClientFactory (recommended for ASP.NET Core)
new LLMConnectClient(LLMConnectClientOptions options, IHttpClientFactory factory)

// Split options — library manages its own HttpClient
new LLMConnectClient(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions? endpointOpts)

// Split options — you supply the HttpClient
new LLMConnectClient(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, HttpClient httpClient)

// Split options — you supply an IHttpClientFactory
new LLMConnectClient(LLMConnectGeneralOptions? generalOpts, LLMConnectEndpointOptions? endpointOpts, IHttpClientFactory factory)
```

> Retry is only automatically attached when LLMConnect creates the `HttpClient`. If you supply your own, configure retry yourself — LLMConnect will log a warning and will not modify it.

### Provider-specific notes

**OpenAI** — auth via `Authorization: Bearer <key>`. Streaming ends with `data: [DONE]`. Full support for `Seed`, `FrequencyPenalty`, `PresencePenalty`, `ResponseFormat`. Full tool calling and embedding support.

**Azure OpenAI** — auth via `api-key` header. The base URL is constructed internally from `AzureResourceName`, `AzureDeploymentName`, and `AzureApiVersion` — you never write the URL yourself. The request/response wire format is identical to OpenAI; the same provider implementation handles both.

**Anthropic** — auth via `x-api-key`. `anthropic-version: 2023-06-01` set automatically. Uses named SSE events (`content_block_delta`, `message_delta`, `message_stop`); no `[DONE]` sentinel. `FinishReason` comes from `message_delta.delta.stop_reason`. **Embeddings not supported** — `GetEmbeddingAsync` throws `NotSupportedException`.

**Google Gemini** — auth via `x-goog-api-key` (never in the URL). Streaming appends `alt=sse` automatically. `FinishReason` comes from `candidates[0].finishReason` on the final chunk. Tool definitions grouped into `functionDeclarations`; `ToolCall.Id` is set to the function name (Google does not issue separate call IDs).

**Ollama** — targets `http://localhost:{port}/api/`. No auth required. Streaming is NDJSON. Tool calling requires a compatible model (e.g. `llama3.1`, `mistral-nemo`). `ToolCall.Id` is set to the function name.

---

## Azure OpenAI

Azure OpenAI is a first-class provider in LLMConnect — not a workaround via a raw endpoint string. The base URL is constructed internally:

```
https://{AzureResourceName}.openai.azure.com/openai/deployments/{AzureDeploymentName}/{path}?api-version={AzureApiVersion}
```

You supply the three named fields; LLMConnect builds the URL and sets the correct `api-key` authentication header. Every other feature — chat, streaming, tool calling, embeddings, retry — works identically to the standard OpenAI provider.

```csharp
var options = new LLMConnectClientOptions
{
    Provider            = ProviderType.AzureOpenAI,
    ApiKey              = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!,
    AzureResourceName   = "my-resource",
    AzureDeploymentName = "gpt-4o-mini-deployment",
    AzureApiVersion     = "2024-10-21"
};

using var client = new LLMConnectClient(options);

// Everything else is identical to OpenAI
var response = await client.ChatAsync(new ChatRequest
{
    Messages = [new UserMessage("Hello from Azure!")]
});
```

**Validation:** when `Provider = AzureOpenAI`, the options validator enforces that `AzureResourceName`, `AzureDeploymentName`, and `AzureApiVersion` are all non-null and non-empty, and that `ApiKey` is present. A clear exception is thrown at construction time if any are missing.

---

## Embeddings

### Basic usage

```csharp
var response = await client.GetEmbeddingAsync(new EmbeddingRequest
{
    Text = "The quick brown fox jumps over the lazy dog."
});

float[] vector = response!.Embedding;
```

If `Model` is not set, `DefaultModel` from options is used. If that is also unset, a per-provider default applies.

### Embeddings provider support matrix

| Feature | OpenAI / Azure | Google | Ollama |
| :--- | :---: | :---: | :---: |
| Basic embedding | ✅ | ✅ | ✅ |
| Custom model | ✅ | ✅ | ✅ |
| Token usage in response | ✅ | ❌ | ❌ |
| Dimensions control | ✅ | ❌ | ❌ |
| Encoding format | ✅ | ❌ | ❌ |
| Task type | ❌ | ✅ | ❌ |
| Title | ❌ | ✅ | ❌ |
| Role | ❌ | ✅ | ❌ |
| Extra options | ✅ | ✅ | ✅ |

### Per-provider embedding options

**OpenAI / Azure:**
```csharp
new EmbeddingRequest
{
    Text           = "Hello, world!",
    Model          = "text-embedding-3-large",
    Dimensions     = 512,
    EncodingFormat = "float",
    User           = "user-abc"
}
```

**Google:**
```csharp
new EmbeddingRequest
{
    Text     = "Document content.",
    Model    = "text-embedding-004",
    TaskType = "RETRIEVAL_DOCUMENT",
    Title    = "My Document",
    Role     = "user"
}
```

**Ollama:**
```csharp
new EmbeddingRequest
{
    Text  = "Embed this locally.",
    Model = "nomic-embed-text",
    ExtraParameters = new Dictionary<string, object> { ["num_ctx"] = 2048 }
}
```

---

## Tool calling

### Defining tools

```csharp
var weatherTool = new Tool
{
    Name        = "get_weather",
    Description = "Returns current temperature and conditions for a city.",
    Parameters  = new Dictionary<string, JsonSchema>
    {
        ["city"] = new JsonSchema
        {
            Type        = "string",
            Description = "The city name."
        },
        ["unit"] = new JsonSchema
        {
            Type = "string",
            Enum = new List<object> { "celsius", "fahrenheit" }
        }
    },
    Required = ["city"]
};
```

Nested types in `JsonSchema`:

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

```csharp
var request = new ChatRequest
{
    Messages = [new UserMessage("What is the weather in Bucharest?")],
    Tools    = [weatherTool]
};

var response = await client.ChatAsync(request);
```

### Handling tool call responses

```csharp
if (response?.ToolCalls?.Count > 0)
{
    foreach (var call in response.ToolCalls)
    {
        Console.WriteLine($"Tool: {call.Name} (id: {call.Id})");

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

```csharp
var response = await client.ChatAsync(request);
var toolCall = response!.ToolCalls![0];

// Execute the tool in your application
var result = await GetWeatherAsync(toolCall.Arguments["city"].ToString()!);

// Return the result to the model
var followUp = new ChatRequest
{
    Messages =
    [
        new UserMessage("What is the weather in Bucharest?"),
        new AssistantMessage(response.Content ?? string.Empty),
        new ToolMessage(toolCallId: toolCall.Id, content: result)
    ],
    Tools = [weatherTool]
};

var finalResponse = await client.ChatAsync(followUp);
Console.WriteLine(finalResponse?.Content);
```

### Tool choice

| Value | Behaviour |
| :--- | :--- |
| `null` | Provider default (usually `"auto"`) |
| `"auto"` | Model decides whether to call a tool |
| `"required"` | Model must call at least one tool |
| `"none"` | Model must not call any tools |
| A tool name | Model must call that specific tool |

```csharp
var request = new ChatRequest
{
    Messages   = [new UserMessage("Get the weather for Bucharest.")],
    Tools      = [weatherTool],
    ToolChoice = "required"
};
```

### Tool calling provider support matrix

| Feature | OpenAI / Azure | Anthropic | Google | Ollama |
| :--- | :---: | :---: | :---: | :---: |
| Basic tool calling | ✅ | ✅ | ✅ | ✅ |
| Multiple tools | ✅ | ✅ | ✅ | ✅ |
| `"auto"` | ✅ | ✅ | ✅ | ✅ |
| `"required"` | ✅ | ✅ | ✅ (→ `ANY`) | ✅ |
| `"none"` | ✅ | ✅ | ✅ | ✅ |
| Named tool | ✅ | ✅ | ✅ | ✅ |
| Parallel calls | ✅ | ✅ | ❌ | model-dependent |

### Tool calling provider-specific notes

**OpenAI / Azure** — tool calls in `message.tool_calls`. `ToolChoice` of a specific name sends `{ "type": "function", "function": { "name": "..." } }`. `ToolCall.Id` is OpenAI's call ID and must be echoed back in `ToolMessage`.

**Anthropic** — tools sent as top-level `tools`. Responses as `tool_use` content blocks. `ToolCall.Id` is Anthropic's `tool_use` ID and must be echoed back in `ToolMessage`.

**Google** — tools grouped into `tools[0].functionDeclarations`. `"required"` maps to `ANY` in `functionCallingConfig.mode`; a specific tool name maps to `ANY` with `allowedFunctionNames`. `ToolCall.Id` is the function name (Google does not issue separate call IDs).

**Ollama** — `ToolCall.Id` is the function name. Works only with models that support tool calling; verify your model before deploying.

---

## Streaming tool calls

`StreamAsync` surfaces tool call deltas alongside text deltas. Accumulate `ArgumentsDelta` across chunks with the same `Index`:

```csharp
var toolArgBuffers = new Dictionary<int, StringBuilder>();
var toolCallMeta   = new Dictionary<int, (string Id, string Name)>();

await foreach (var chunk in client.StreamAsync(request))
{
    // Handle text content
    if (!string.IsNullOrEmpty(chunk.Content))
        Console.Write(chunk.Content);

    // Accumulate tool call argument fragments
    if (chunk.ToolCalls != null)
    {
        foreach (var delta in chunk.ToolCalls)
        {
            if (!toolArgBuffers.ContainsKey(delta.Index))
            {
                toolArgBuffers[delta.Index] = new StringBuilder();
                toolCallMeta[delta.Index]   = (delta.Id ?? "", delta.Name ?? "");
            }

            toolArgBuffers[delta.Index].Append(delta.ArgumentsDelta);
        }
    }

    // When stream ends, deserialize accumulated arguments
    if (chunk.IsComplete && toolArgBuffers.Count > 0)
    {
        foreach (var (index, buffer) in toolArgBuffers)
        {
            var (id, name) = toolCallMeta[index];
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(buffer.ToString());
            Console.WriteLine($"\nTool call: {name} (id: {id}), args: {buffer}");
        }
    }
}
```

---

## Dependency injection

Two `AddLLMConnect` overloads are available.

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
        general.Provider     = ProviderType.AzureOpenAI;
        general.ApiKey       = builder.Configuration["Azure:ApiKey"]!;
        general.DefaultModel = "gpt-4o-mini";
    },
    configureEndpoint: endpoint =>
    {
        endpoint.AzureResourceName   = builder.Configuration["Azure:ResourceName"];
        endpoint.AzureDeploymentName = builder.Configuration["Azure:DeploymentName"];
        endpoint.AzureApiVersion     = builder.Configuration["Azure:ApiVersion"];
    }
);
```

Both register a named `HttpClient` (`"LLMConnect"`) with connection pooling and the retry handler, and `ILLMConnectClient` as a singleton.

```csharp
public class MyService(ILLMConnectClient client)
{
    public Task<ChatResponse?> AskAsync(string question) =>
        client.ChatAsync(new ChatRequest { Messages = [new UserMessage(question)] });

    public Task<EmbeddingResponse?> EmbedAsync(string text) =>
        client.GetEmbeddingAsync(new EmbeddingRequest { Text = text });
}
```

---

## Retry behavior

Retried on:

- HTTP `429 Too Many Requests`
- HTTP `5xx` server errors
- `HttpRequestException` (network-level failures)

Uses exponential backoff with jitter via Polly, up to `MaxRetries` attempts. Each retry is logged at `Warning` when a `LoggerFactory` is configured.

**Known caveats:**

- `Retry-After` headers are not currently honored; backoff is always computed locally.
- A timeout-triggered retry may result in the provider billing for the same call twice, as LLMConnect does not send idempotency keys.
- Retry is only automatically attached when LLMConnect owns the `HttpClient`.

---

## Error handling

All provider errors surface as `LLMConnectException`:

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
    // Thrown by GetEmbeddingAsync when Provider = Anthropic
    Console.WriteLine(ex.Message);
}
```

LLMConnect extracts a human-readable message from the provider's JSON error body. If the body is not valid JSON (e.g. an HTML error page from a proxy), it falls back to the HTTP status code and raw body text rather than throwing a `JsonException`.

---

## Known limitations

- **No batch embedding support.** `EmbeddingRequest.Text` accepts a single string.
- **Anthropic does not support embeddings.** `GetEmbeddingAsync` throws `NotSupportedException` for the Anthropic provider.
- **`Retry-After` not honored** on `429` responses.
- **Ollama tool calling is model-dependent.** Not all Ollama models support tool calling.
- **`FinishReason` varies by provider.** See the provider-specific notes for what each provider signals on stream completion.

---

## Roadmap

### Now
- Chat completions (non-streaming and streaming) — OpenAI, Azure OpenAI, Anthropic, Google, Ollama
- Vector embeddings — OpenAI, Azure OpenAI, Google, Ollama
- Tool/function calling with streaming deltas — all five providers
- Azure OpenAI as a first-class provider
- Split configuration (`LLMConnectGeneralOptions` + `LLMConnectEndpointOptions`)
- Retry with exponential backoff and jitter
- DI support with two registration overloads

### Next
- Honor `Retry-After` on rate-limit responses
- Batch embedding support
- Per-request provider selection

### Later
- `Microsoft.Extensions.AI` integration
- Additional providers (e.g. Mistral, Cohere)

---

## Contributing

Please open an issue before submitting a pull request for significant changes. The test suite (`LLMConnect.Tests`) uses xUnit and WireMock for provider integration tests that run without real network calls.

```bash
git clone https://github.com/bargross/llm-connect.git
cd llm-connect/LLMConnect
dotnet test
```

---

## License

Apache License, Version 2.0. See [LICENSE](./LICENSE) for details.