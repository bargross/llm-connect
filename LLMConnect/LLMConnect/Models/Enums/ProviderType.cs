namespace LLMConnect.Models;

/// <summary>
/// Identifies which LLM provider a <see cref="LLMConnect.Settings.LLMConnectClientOptions"/>
/// instance or request should target.
/// </summary>
public enum ProviderType
{
    /// <summary>OpenAI's Chat Completions API.</summary>
    OpenAI,

    /// <summary>Anthropic's Messages API.</summary>
    Anthropic,

    /// <summary>Google's Gemini (Generative Language) API.</summary>
    Google,

    /// <summary>A locally hosted Ollama server.</summary>
    Ollama
}