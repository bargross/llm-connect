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
