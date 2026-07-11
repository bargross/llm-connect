# Changelog

All notable changes to LLMConnect are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [2.0.0] - Release

This release adds three major features — embeddings, tool/function calling, and Azure OpenAI — alongside a significant configuration refactor and a series of bug fixes carried over from the v1 review cycle.

### Added

#### Azure OpenAI provider (`ProviderType.AzureOpenAI`)
- First-class `AzureOpenAI` provider type — not a raw endpoint override
- Base URL constructed internally from three named options: `AzureResourceName`, `AzureDeploymentName`, and `AzureApiVersion`
- Authentication uses `api-key` header (not `Authorization: Bearer`)
- Wire format is identical to OpenAI; the same provider implementation handles both, with no branching in chat, streaming, tool calling, or embeddings
- Dedicated `AzureOpenAIOptionsValidator` enforces all three Azure fields are present at construction time

#### Embeddings (`GetEmbeddingAsync`)
- New `ILLMConnectClient.GetEmbeddingAsync(EmbeddingRequest, CancellationToken)` method
- New `EmbeddingRequest` model with provider-agnostic fields (`Text`, `Model`) and provider-specific fields (`Dimensions`, `EncodingFormat` for OpenAI/Azure; `TaskType`, `Title`, `Role` for Google; `ExtraParameters` escape hatch for all)
- New `EmbeddingResponse` model (`Embedding: float[]`, `Model`, `Usage`, `CreatedAt`)
- Supported providers: OpenAI, Azure OpenAI, Google Gemini, Ollama
- Anthropic throws `NotSupportedException` (not supported by the Anthropic API)
- Per-provider request/response mapping extensions: `ToOpenAIEmbeddingRequest`, `ToGoogleEmbeddingRequest`, `ToOllamaEmbeddingRequest`, and their response counterparts
- `EmbeddingRequestValidatorBase` and per-provider validators (OpenAI, Google, Ollama, Anthropic)
- `EmbeddingRequestValidatorFactory` resolves the correct validator per provider

#### Tool/function calling
- New `Tool` model: `Name`, `Description`, `Parameters: Dictionary<string, JsonSchema>`, `Required: List<string>`
- New `JsonSchema` model: `Type`, `Description`, `Items`, `Properties`, `Enum`, `Extra` — supports nested object and array types
- `ChatRequest.Tools` and `ChatRequest.ToolChoice` (`"auto"`, `"required"`, `"none"`, or a specific tool name)
- `ChatResponse.ToolCalls: List<ToolCall>` — fully deserialized arguments, populated when the model invokes a tool
- New `ToolCall` model: `Id`, `Name`, `Arguments: Dictionary<string, object>`
- Tool call mapping for all five providers:
  - OpenAI/Azure: `tools` array with `function` type wrappers; `tool_choice` with named-tool support
  - Anthropic: `tools` array; `tool_choice` mapped to Anthropic's `{ type: "auto|any|tool" }` format
  - Google: `tools[0].functionDeclarations`; `tool_choice` mapped to `functionCallingConfig.mode` (`AUTO`, `ANY`, `NONE`) with `allowedFunctionNames` for named-tool selection
  - Ollama: `tools` array with `function` wrappers; `tool_choice` forwarded as-is
- Response mapping extracts tool calls from each provider's wire format:
  - OpenAI/Azure: `message.tool_calls[].function`
  - Anthropic: `content[]` blocks of type `tool_use`
  - Google: `candidates[0].content.parts[]` of type `functionCall`
  - Ollama: `message.tool_calls[].function`

#### Streaming tool call deltas (`ToolCallDelta`)
- New `ChatChunk.ToolCalls: List<ToolCallDelta>` — populated during streamed tool calls
- New `ToolCallDelta` model: `Index`, `Id`, `Name`, `ArgumentsDelta` (partial JSON fragment for this chunk only)
- Per-provider streaming parser updates:
  - **OpenAI/Azure**: maps `choices[0].delta.tool_calls[]` to `ToolCallDelta`; `Id` and `Name` present on first delta per index
  - **Anthropic**: stateful parser accumulates `content_block_start` (captures `Id`, `Name`) then streams `input_json_delta` fragments — correct delta type name (`input_json_delta`, not the previously incorrect `tool_use_delta`); `ArgumentsDelta` carries only the new fragment per chunk, not the accumulated string; `FinishReason` populated from `message_delta.delta.stop_reason`
  - **Google**: iterates all `functionCall` parts in a chunk (not just the first); serializes `fc.Args` to JSON as `ArgumentsDelta`
  - **Ollama**: maps `message.tool_calls[]`; serializes `Function.Arguments` to JSON as `ArgumentsDelta`

#### Split configuration
- New `LLMConnectGeneralOptions` — identity/auth/retry settings
- New `LLMConnectEndpointOptions` — endpoint settings (`AzureResourceName`, `AzureDeploymentName`, `AzureApiVersion`, `OllamaPort`)
- `LLMConnectClientOptions` retains all fields for single-object configuration
- Six `LLMConnectClient` constructors: three for unified options (no external `HttpClient`, external `HttpClient`, `IHttpClientFactory`) × mirrored three for split options
- Two `AddLLMConnect` DI overloads: unified options and split options
- `EndpointRegistry` refactored — `GetDefaultEndpoint` now builds base URLs from options fields rather than looking up static strings; Azure URL built from resource/deployment/version; Ollama port substituted from `OllamaPort`

### Changed

- `EndpointRegistry` restructured with separate `_domains`, `_apiVersions`, and `_relativePaths` dictionaries; `GetEndpointParams` now takes `(ProviderType, QueryType, bool isStreaming, string? model)` — streaming and non-streaming no longer require separate lookup logic in providers
- `ProviderBase` is now `ProviderBase<TProvider>` — each provider passes itself as the generic parameter, so `ILogger<TProvider>` is correctly scoped per provider (previously all providers logged under `OpenAIProvider`)
- `LLMProviderFactory` updated to instantiate `OpenAIProvider` for both `ProviderType.OpenAI` and `ProviderType.AzureOpenAI`
- `HttpClientConfigurator` updated to set `api-key` header for Azure, construct the Azure base URL from named options, and substitute the Ollama port
- `Usage.TotalTokens` is now a computed property (`=> InputTokens + OutputTokens`) — the previously duplicate settable `TotalTokens` field and separate computed `TotalTokenCount` are consolidated into one
- All providers' `ChatAsync` methods no longer call `ReadAsStringAsync` before `DeserializeResponseAsync` — the redundant pre-read that consumed the response stream before deserialization is removed
- `GoogleStreamChunkParser` now iterates all parts in a candidate rather than using `FirstOrDefault`, correctly handling multiple `functionCall` parts in a single chunk

### Removed

- `LLMConnectEndpointOptions.Endpoint` — raw endpoint override removed; Azure URL is constructed from named fields; Ollama uses `OllamaPort`; all other providers use their hardcoded default endpoints
- `ProviderBase.DeserializeChatResponseAsync` — branching custom/standard deserialization method removed
- `ProviderBase.DeserializeEmbeddingResponseAsync` — same
- `ProviderBase.ReadFromStreamAsync` — custom stream reader branching removed

### Fixed

- **Anthropic streaming — wrong delta type name**: `tool_use_delta` corrected to `input_json_delta` (Anthropic's actual wire format); the previous name made the branch unreachable, silently dropping all Anthropic streaming tool calls
- **Anthropic streaming — accumulated arguments in delta**: `ArgumentsDelta` was incorrectly set to the full accumulated string on every chunk; now correctly set to only the new partial fragment for each chunk
- **`HasCustomChatDeserializer` / `HasCustomEmbeddingDeserializer` inverted** (now moot — both properties removed along with the custom deserialization feature)
- **`GetResponse<TResult>` un-interpolated error message**: `"Failed to deserialize response due to: {ex.Message}"` was a literal string with no interpolation — `ex.Message` was never inserted. Fixed with `$` prefix
- **`ProviderBase` logger category**: all providers previously logged under `ILogger<OpenAIProvider>` — fixed via generic `ProviderBase<TProvider>`
- **Google `ChatAsync` URL construction**: now uses `GetEndpointParams` consistently rather than building from `BaseAddress` directly, which previously omitted the model name and `:generateContent` path segment
- **Google embedding endpoint — missing `{model}` substitution**: `GetEmbeddingAsync` was posting to the literal `{model}:embedContent` path; now correctly substitutes the model name
- **`Usage.TotalTokens` inconsistency**: settable `TotalTokens` was `0` for providers that did not explicitly set it; now computed from `InputTokens + OutputTokens` everywhere
- **`OllamaEmbeddingRequest.Prompt` nullability**: `Prompt = request?.Text` could assign null to a non-nullable field; addressed by validation guaranteeing `Text` is non-null before mapping
- **Redundant `ReadAsStringAsync` in `ChatAsync`**: OpenAI, Google, and Ollama providers called `ReadAsStringAsync` before passing the response to `DeserializeResponseAsync`, which also calls it — the second read returned an empty string, making deserialization always fail. The redundant read is removed
- **Double-stacked retry pipelines**: `ServiceCollectionExtensions` previously registered both `RetryDelegatingHandler` and `AddResilienceHandler`, causing up to 16 HTTP attempts per logical call. Now uses a single retry layer
- **`RetryDelegatingHandler` — no jitter**: backoff was pure `2^attempt` seconds; now uses Polly's `UseJitter = true`
- **`RetryDelegatingHandler` — policy allocated per call**: `ResiliencePipeline` is now built once in the constructor rather than on every `SendAsync` invocation
- **`RetryDelegatingHandler` — missing logger**: retry attempts were never logged; `ILogger` is now passed in and retries log at `Warning` with attempt number, delay, and failure reason
- **Stale TODO comments in `ChatRequest`**: two leftover `TODO` comments (a commented-out `Tools` property and a per-request provider selection note) are cleaned up

---

## [1.0.0] — Initial release

### Added

- `ILLMConnectClient` with `ChatAsync` and `StreamAsync`
- Providers: OpenAI, Anthropic, Google Gemini, Ollama
- Streaming via SSE (OpenAI, Anthropic, Google) and NDJSON (Ollama)
- Shared `IStreamEventReader` / `IStreamChunkParser` abstraction — one SSE loop, one NDJSON loop, per-provider chunk parsers
- `LLMConnectClientOptions` with `Provider`, `ApiKey`, `DefaultModel`, `Timeout`, `MaxRetries`, `LoggerFactory`
- `RetryDelegatingHandler` with Polly-backed exponential backoff
- `HttpClientConfigurator` with `SocketsHttpHandler.PooledConnectionLifetime` for DNS-staleness mitigation
- `IDisposable` on `LLMConnectClient` — disposes self-created `HttpClient` only
- `ProviderBase` with shared `ExtractErrorMessage` (handles non-JSON error bodies gracefully) and `LogAndThrow`
- `EndpointRegistry` with hardcoded default endpoints per provider
- Google `x-goog-api-key` header authentication (key removed from URL)
- Google streaming `alt=sse` appended automatically
- `AddLLMConnect` DI extension with named `HttpClient` and retry handler
- Options validation with per-provider validators
- XML documentation on all public types and members