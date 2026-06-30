[![NuGet Version](https://img.shields.io/nuget/v/llm-connect)](https://www.nuget.org/packages/llm-connect/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Build Status](https://img.shields.io/github/actions/workflow/status/bargross/llm-connect/dotnet.yml?branch=main)](https://github.com/bargross/llm-connect/actions)

# LLMConnect

A provider‑agnostic .NET client for Large Language Models. Write your chat logic once and run it against OpenAI, Anthropic, Google Gemini, or a local Ollama server through a single, consistent API.

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
  - [Usage](#usage)
- [Configuration](#configuration)
  - [LLMConnectClientOptions reference](#llmconnectclientoptions-reference)
  - [Choosing a constructor](#choosing-a-constructor)
  - [Provider‑specific notes](#provider-specific-notes)
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

LLMConnect is a unified client library for .NET that abstracts away the differences between multiple LLM providers. It gives you one interface — `ILLMConnectClient` — for chat completions and streaming, regardless of which provider sits behind it.

Stop learning a new SDK every time you want to switch providers. Write your application logic once against `ChatRequest` / `ChatResponse` / `ChatChunk`, and change providers with a single configuration value.

---

## Features

- Provider‑agnostic core: one request/response model shape for OpenAI, Anthropic, Google Gemini, and Ollama
- Non‑streaming (`ChatAsync`) and streaming (`StreamAsync`) chat completions
- Built‑in retry with exponential backoff and jitter, backed by [Polly](https://github.com/App-vNext/Polly)
- Dependency Injection support via `Microsoft.Extensions.DependencyInjection`
- Per‑instance default model, configurable timeout and retry count
- Strongly typed message roles (`SystemMessage`, `UserMessage`, `AssistantMessage`, `ToolMessage`)
- Optional structured logging via `Microsoft.Extensions.Logging`
- Full async/await and `IAsyncEnumerable` support for streaming
- Targets .NET 10

---

## Supported providers

| Provider | Non‑Streaming | Streaming | Auth |
| :--- | :---: | :---: | :--- |
| OpenAI | ✅ | ✅ | `Authorization: Bearer <key>` |
| Anthropic | ✅ | ✅ | `x-api-key` header |
| Google Gemini | ✅ | ✅ | `x-goog-api-key` header |
| Ollama (local) | ✅ | ✅ | none |

Streaming uses Server‑Sent Events (SSE) for OpenAI, Anthropic, and Google, and newline‑delimited JSON (NDJSON) for Ollama. This is handled internally — you consume the same `IAsyncEnumerable<ChatChunk>` regardless of provider.

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

```csharp
using LLMConnect;
using LLMConnect.Models;
using LLMConnect.Settings;

var options = new LLMConnectClientOptions
{
    Provider = ProviderType.OpenAI,
    ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
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

Streaming looks the same, but consumes an `IAsyncEnumerable<ChatChunk>`:

```csharp
await foreach (var chunk in client.StreamAsync(request))
{
    Console.Write(chunk.Content);

    if (chunk.IsComplete)
        Console.WriteLine($"\n[finished: {chunk.FinishReason}]");
}
```

Switching providers is a configuration change, not a code change:

```csharp
var options = new LLMConnectClientOptions
{
    Provider = ProviderType.Anthropic,
    ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!,
    DefaultModel = "claude-3-5-sonnet-20241022"
};
```

---

## Core concepts

### The client

The entry point is `ILLMConnectClient`, implemented by `LLMConnectClient`. It exposes two methods:

```csharp
Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
```

`LLMConnectClient` implements `IDisposable`. If the client created its own internal `HttpClient` (i.e. you used the options‑only constructor), disposing the client disposes that `HttpClient` too. If you supplied your own `HttpClient` or an `IHttpClientFactory`, LLMConnect will not dispose it — you remain the owner.

### Messages

`ChatRequest.Messages` is a `List<Message>`. `Message` is abstract; construct one of the concrete role types instead:

```csharp
new SystemMessage("You are a helpful assistant.");
new UserMessage("Hello!");
new AssistantMessage("Hi, how can I help?");
new ToolMessage(toolCallId: "call_123", content: "{\"result\": 42}");
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
    public string? Provider { get; set; }
    public List<string>? StopSequences { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
    public string? ResponseFormat { get; set; } // "text" or "json_object"
    public int? Seed { get; set; }
    public string? User { get; set; }
    public Dictionary<string, object>? ExtraParameters { get; set; }
}
```

Notes:

- `Model` overrides `LLMConnectClientOptions.DefaultModel` for a single request.
- Not every provider supports every field (e.g. `Seed` and `FrequencyPenalty`/`PresencePenalty` are OpenAI‑specific concepts). Providers that don't understand a field simply ignore it rather than failing the request.
- `ExtraParameters` is serialized as additional top‑level JSON properties on the outgoing request (`[JsonExtensionData]`), so you can pass provider‑specific options LLMConnect doesn't model explicitly without waiting for a library update.
- Function/tool calling (`Tools` on the request) is **not implemented yet** — see [Known limitations](#known-limitations).

### ChatResponse

Returned by `ChatAsync` for non‑streaming calls:

```csharp
public class ChatResponse
{
    public string? Content { get; set; }
    public string? FinishReason { get; set; }
    public Usage Usage { get; set; } = new();
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### ChatChunk (streaming)

Returned by `StreamAsync`, one instance per streamed delta:

```csharp
public class ChatChunk
{
    public string? Content { get; set; }
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
}
```

`IsComplete` is `true` on the final chunk of the stream. `FinishReason` is only populated on that final chunk (where the provider's wire format makes it available — see the per‑provider notes below for what each provider actually signals).

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

### LLMConnectClientOptions reference

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Provider` | `ProviderType` | `OpenAI` | Which provider to target: `OpenAI`, `Anthropic`, `Google`, `Ollama`. |
| `ApiKey` | `string` | `""` | Provider API key. Not required for `Ollama`. |
| `DefaultModel` | `string?` | `null` | Model used when `ChatRequest.Model` is not set. |
| `Endpoint` | `string?` | `null` | Override the default endpoint URL for the provider. Takes precedence over `OllamaPort` if both are set. |
| `OllamaPort` | `int?` | `null` (→ `11434`) | Port for a local Ollama server. Ignored if `Endpoint` is set. |
| `Timeout` | `TimeSpan` | `60s` | Per‑request HTTP timeout. |
| `MaxRetries` | `int` | `3` | Maximum retry attempts on transient failures (must be `>= 0`). |
| `LoggerFactory` | `ILoggerFactory?` | `null` | Optional. If provided, LLMConnect emits structured logs (retries, errors) through it. |
| `ExtraOptions` | `Dictionary<string, object>?` | `null` | Reserved for future provider‑specific configuration. |

### Choosing a constructor

`LLMConnectClient` has three constructors, each suited to a different hosting scenario:

```csharp
// 1. Library manages its own HttpClient (simplest option for console apps, scripts, tests)
new LLMConnectClient(options);

// 2. You manage the HttpClient yourself (you own its lifetime and any handlers)
new LLMConnectClient(options, httpClient);

// 3. You provide an IHttpClientFactory (recommended for ASP.NET Core / long-running services)
new LLMConnectClient(options, httpClientFactory);
```

> **Important:** retry behavior differs by constructor. The options‑only constructor (1) and a manually constructed factory path both attach LLMConnect's own retry handler automatically. If you pass in your **own** `HttpClient` (2), you are responsible for configuring retry/resilience yourself — LLMConnect will not add a retry handler to a client it doesn't own, and will log a warning to that effect if a logger is configured.

### Provider‑specific notes

**OpenAI** — streaming ends with a literal `data: [DONE]` sentinel on the wire; `ChatChunk.FinishReason` is populated from the final non‑`[DONE]` chunk's `finish_reason` field when present.

**Anthropic** — requires `anthropic-version` (LLMConnect sets this automatically) and uses named SSE events (`content_block_delta`, `message_stop`, etc.) rather than a `[DONE]` sentinel. The stream ends when the connection closes after `message_stop`.

**Google Gemini** — streaming requests automatically append `alt=sse` to the endpoint so the API returns SSE instead of a buffered JSON array. There is no explicit "done" event; instead, the **final** chunk's `candidates[0].finishReason` field (e.g. `STOP`, `MAX_TOKENS`, `SAFETY`) signals completion, and LLMConnect surfaces that onto the final `ChatChunk.FinishReason` / `IsComplete`. The API key is sent via the `x-goog-api-key` header, not embedded in the URL.

**Ollama** — talks to a local server (`http://localhost:11434` by default) and requires no API key. Streaming is NDJSON, not SSE — each line is a complete JSON object, and the final line carries `"done": true`.

---

## Dependency injection

LLMConnect integrates with `Microsoft.Extensions.DependencyInjection` via `AddLLMConnect`:

```csharp
using LLMConnect.Configuration;

builder.Services.AddLLMConnect(options =>
{
    options.Provider = ProviderType.OpenAI;
    options.ApiKey = builder.Configuration["OpenAI:ApiKey"]!;
    options.DefaultModel = "gpt-4o-mini";
    options.MaxRetries = 3;
});
```

This registers:

- A named `HttpClient` (`"LLMConnect"`) configured with connection pooling (`SocketsHttpHandler.PooledConnectionLifetime = 5 minutes`, mitigating DNS‑staleness issues with long‑lived clients) and LLMConnect's retry handler.
- `ILLMConnectClient` as a singleton, resolved through `IHttpClientFactory` under the hood.

Inject and use it like any other service:

```csharp
public class MyService(ILLMConnectClient client)
{
    public Task<ChatResponse?> AskAsync(string question) =>
        client.ChatAsync(new ChatRequest
        {
            Messages = [new UserMessage(question)]
        });
}
```

---

## Retry behavior

LLMConnect retries requests that fail with:

- HTTP `429 Too Many Requests`
- HTTP `5xx` server errors
- `HttpRequestException` (network‑level failures, e.g. connection reset, DNS failure)

Retries use exponential backoff with jitter (powered by Polly's `ResiliencePipeline`), up to `MaxRetries` attempts (default `3`, configurable, `0` disables retries entirely). If a `LoggerFactory` is configured, each retry attempt is logged at `Warning` level with the attempt number, delay, and failure reason.

**Things to be aware of:**

- Retries are **not** currently aware of the `Retry-After` header some providers return on `429` responses; backoff is always computed locally rather than honoring the provider's suggested wait time.
- A retried request that failed due to a timeout (rather than a clear error response) may have already been processed server‑side by the provider before the client gave up waiting. LLMConnect does not currently send idempotency keys, so a timeout‑triggered retry can, in rare cases, result in the provider billing for more than one completion for what is logically a single call. If this matters for your use case, consider setting a generous `Timeout` and a conservative `MaxRetries`.
- Retry behavior is only attached automatically when LLMConnect owns the `HttpClient` (constructors 1 and 3 above, and the DI registration). If you supply your own `HttpClient`, you must configure retry/resilience yourself.

---

## Streaming

`StreamAsync` returns `IAsyncEnumerable<ChatChunk>` and can be consumed with `await foreach`:

```csharp
var sb = new StringBuilder();

await foreach (var chunk in client.StreamAsync(request, cancellationToken))
{
    sb.Append(chunk.Content);
}
```

Internally, streaming is implemented as two small, provider‑independent layers:

- An **event reader** that understands the wire protocol (SSE for OpenAI/Anthropic/Google, NDJSON for Ollama) and yields raw `(EventName, Data)` pairs.
- A **chunk parser**, one per provider, that turns a raw event into a `ChatChunk` (or discards it, for non‑content events like SSE comments or Anthropic's non‑delta events).

This means parsing the wire protocol itself is implemented once per protocol, not once per provider — and you don't need to know any of this to use the library; it's mentioned here for anyone extending LLMConnect with a new provider.

Cancellation is supported throughout: pass a `CancellationToken` to `StreamAsync`, and the underlying read loop will stop cleanly when it's triggered.

---

## Error handling

All provider errors are surfaced as `LLMConnectException`:

```csharp
public class LLMConnectException : Exception
{
    public string? Provider { get; set; }
    // + standard Exception constructors, with/without Provider and InnerException
}
```

```csharp
try
{
    var response = await client.ChatAsync(request);
}
catch (LLMConnectException ex)
{
    Console.WriteLine($"[{ex.Provider}] request failed: {ex.Message}");
}
```

LLMConnect attempts to extract a human‑readable message from the provider's JSON error body (e.g. OpenAI/Anthropic's `error.message`, Google's `error.message`). If the response body isn't valid JSON (e.g. an HTML error page from a proxy, or an empty body), LLMConnect falls back to a message containing the raw HTTP status code and body text rather than throwing an unrelated JSON parsing exception.

---

## Known limitations

LLMConnect is under active development. Current known gaps:

- **No function/tool calling support yet.** `ToolMessage` exists for representing tool results in conversation history, but there is no way to declare available tools/functions on a `ChatRequest` or receive a structured tool‑call request back from the model. Planned for a future release.
- **No embeddings API.** Only chat completions are supported.
- **`Retry-After` is not honored** on `429` responses — see [Retry behavior](#retry-behavior).
- **Streaming finish‑reason fidelity varies by provider** — see the [provider‑specific notes](#provider-specific-notes) for what each provider actually signals on stream completion.

If you hit a gap not listed here, please open an issue.

---

## Roadmap

### Now
- Chat completions (non‑streaming and streaming) across OpenAI, Anthropic, Google, and Ollama
- Consistent request/response models
- Retry with backoff and jitter, DI support

### Next
- Honor `Retry-After` on rate‑limit responses
- Tool/function calling support
- Embeddings API

### Later
- `Microsoft.Extensions.AI` integration
- Additional providers (e.g. Mistral, Cohere)

---

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a pull request, and include tests for new behavior — the project has an xUnit test suite (`LLMConnect.Tests`) covering providers, streaming, retry behavior, and configuration validation, including WireMock‑based integration tests that simulate all four providers without making real network calls.

```bash
git clone https://github.com/bargross/llm-connect.git
cd llm-connect/LLMConnect
dotnet test
```

---

## License

This project is licensed under the Apache License, Version 2.0. See the [LICENSE](./LICENSE) file for details.
