namespace LLMConnect.Models;

/// <summary>
/// Represents a JSON schema for defining the structure of JSON data.
/// </summary>
public class JsonSchema
{
    /// <summary>
    /// Gets or sets the type of the JSON schema (e.g., "string", "number", "object", "array").
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Gets or sets the description of the JSON schema, providing additional information about the schema's purpose or usage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the items schema for arrays, defining the structure of the elements within the array.
    /// </summary>
    public JsonSchema? Items { get; set; } // for arrays

    /// <summary>
    /// Gets or sets the properties of the JSON schema for objects, defining the structure of the object's properties and their corresponding schemas.
    /// </summary>
    public Dictionary<string, JsonSchema>? Properties { get; set; } // for nested objects

    /// <summary>
    /// Gets or sets the list of allowed values for the JSON schema, providing a set of valid options for the schema's value.
    /// </summary>
    public List<object>? Enum { get; set; }

    /// <summary>
    /// Gets or sets any additional properties for the JSON schema, allowing for the inclusion of extra metadata or custom attributes.
    /// </summary>
    public Dictionary<string, object>? Extra { get; set; }
}