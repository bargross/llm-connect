[![NuGet Version](https://img.shields.io/nuget/v/llm-connect)](https://www.nuget.org/packages/llm-connect/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Build Status](https://img.shields.io/github/actions/workflow/status/bargross/llm-connect/dotnet.yml?branch=main)](https://github.com/bargross/llm-connect/actions)

# LLMConnect

A provider‑agnostic .NET client for Large Language Models. Write once, run on OpenAI, Anthropic, Google, and Ollama through a single, consistent API.

---

## What is LLMConnect?

LLMConnect is a unified client library for .NET that abstracts away the differences between multiple LLM providers. It gives you one interface for chat completions and streaming, regardless of which provider you use.

Stop learning a new SDK every time you want to switch providers. Write your application logic once, and change providers with a single configuration line.

---

## Features

- Provider‑agnostic core with zero external dependencies
- Support for OpenAI, Anthropic, Google Gemini, and Ollama
- Non‑streaming and streaming chat completions
- Consistent request/response models across all providers
- Built‑in retry logic with exponential backoff
- Dependency Injection ready (Microsoft.Extensions.DependencyInjection)
- Configurable timeouts and retries
- Default model support per client instance
- Full async/await support
- Framework agnostic – works with .NET 8 and above

---

## Installation

Install the package via NuGet:

```nuget
dotnet add package LLMConnect
```

Or via Package Manager Console.


---

## Supported Providers

| Provider | Non‑Streaming | Streaming |
| :--- | :--- | :--- |
| OpenAI | ✅ Yes | ✅ Yes |
| Anthropic | ✅ Yes | ✅ Yes |
| Google Gemini | ✅ Yes | ✅ Yes |
| Ollama | ✅ Yes | ✅ Yes |

---

## Core Concepts

### The Client

The main entry point is `ILLMClient`, implemented by `LLMClient`. You create an instance with configuration options, then call `ChatAsync` or `StreamAsync`.

### ChatRequest

This object holds everything needed for a chat completion. It includes:

- Messages (system, user, assistant)
- System prompt
- Temperature, TopP, MaxTokens
- Model name (optional; falls back to the default)
- Stop sequences, frequency penalty, presence penalty
- Response format, seed, user identifier
- Extra parameters for provider‑specific options

### ChatResponse

The response contains the generated content, finish reason, token usage, and the model that was used.

### ChatChunk

For streaming, the response is yielded as a sequence of `ChatChunk` objects, each containing a piece of the response text and a flag indicating whether the stream is complete.

### Provider Types

The `ProviderType` enum lets you specify which provider to use:

- `ProviderType.OpenAI`
- `ProviderType.Anthropic`
- `ProviderType.Google`
- `ProviderType.Ollama`

---

## Configuration

### Basic Configuration

Create an instance of `LLMClientOptions` with the provider and API key:

- Provider – the provider you want to use
- ApiKey – your API key (not required for Ollama)
- DefaultModel – the model to use if not specified in the request
- Endpoint – optional custom endpoint override
- Timeout – request timeout (default: 60 seconds)
- MaxRetries – number of retry attempts (default: 3)

### Ollama‑Specific Configuration

For Ollama, you can also set the port:

- OllamaPort – the port Ollama is running on (default: 11434)

If you provide both `Endpoint` and `OllamaPort`, the `Endpoint` takes precedence.

### Dependency Injection

LLMConnect integrates with `Microsoft.Extensions.DependencyInjection`. You can register the client using the `AddLLMConnect` extension method with your configuration.

---

## Request Properties

The `ChatRequest` class supports the following common parameters:

- Messages – the conversation history
- SystemPrompt – the system instruction
- Temperature – controls randomness (0.0 to 1.0)
- TopP – nucleus sampling threshold
- MaxTokens – maximum tokens to generate
- Model – the model name (overrides the default)
- StopSequences – list of strings to stop generation
- FrequencyPenalty – penalizes repeated tokens
- PresencePenalty – penalizes tokens that have already appeared
- ResponseFormat – "text" or "json_object"
- Seed – for deterministic generation
- User – user identifier for abuse monitoring
- ExtraParameters – additional provider‑specific options

Not all providers support every parameter. Unsupported parameters are safely ignored or handled via `ExtraParameters`.

---

## Roadmap

### Version 1.0 (Current)
- Chat completions (non‑streaming and streaming)
- Support for OpenAI, Anthropic, Google, and Ollama
- Consistent request/response models
- Built‑in retry and error handling
- Dependency Injection support

### Version 1.1 (Planned)
- Full Anthropic SSE streaming (already implemented)
- Embeddings API
- Improved error messages

### Version 1.2 (Planned)
- Tool/function calling support
- Response schema validation

### Version 2.0 (Future)
- Microsoft.Extensions.AI integration
- Additional providers (Mistral, Cohere, etc.)

---

## Contributing

Contributions are welcome. Please open an issue or pull request on GitHub.

---

## License

This project is licensed under the Apache License, Version 2.0. See the LICENSE file for details.
