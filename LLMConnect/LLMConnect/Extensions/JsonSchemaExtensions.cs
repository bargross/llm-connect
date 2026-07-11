using LLMConnect.Models;

namespace LLMConnect;

internal static class JsonSchemaExtensions
{
    public static Dictionary<string, object> ToDictionary(this JsonSchema schema)
    {
        var dict = new Dictionary<string, object>
        {
            ["type"] = schema.Type
        };

        if (!string.IsNullOrEmpty(schema.Description))
            dict["description"] = schema.Description;

        if (schema.Properties != null && schema.Properties.Count > 0)
        {
            var props = new Dictionary<string, object>();
            foreach (var kvp in schema.Properties)
                props[kvp.Key] = kvp.Value.ToDictionary();
            dict["properties"] = props;
        }

        if (schema.Items != null)
            dict["items"] = schema.Items.ToDictionary();

        if (schema.Enum != null && schema.Enum.Count > 0)
            dict["enum"] = schema.Enum;

        if (schema.Extra != null && schema.Extra.Count > 0)
        {
            foreach (var kvp in schema.Extra)
                dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }
}