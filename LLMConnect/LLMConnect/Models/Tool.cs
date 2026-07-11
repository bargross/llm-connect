using System.Text.Json.Serialization;

namespace LLMConnect.Models;

/// <summary>
/// Represents a tool that can be used by the LLM. This is typically used in the context of a "function call" in OpenAI's API, where the model can call a function (tool) with specific parameters.
/// </summary>
public class Tool
{
    /// <summary>
    /// The name of the tool. This is typically used as the identifier for the tool when the LLM wants to invoke it.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what the tool does. This can be used to provide context to the LLM about the purpose of the tool.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// A dictionary of property names to their JSON Schema definitions.
    /// This represents the "properties" of the tool's parameter object.
    /// The top-level type is always "object".
    /// </summary>
    public Dictionary<string, JsonSchema> Parameters { get; set; } = new();

    /// <summary>
    /// Optional list of property names that are required.
    /// </summary>
    public List<string> Required { get; set; } = new();
}