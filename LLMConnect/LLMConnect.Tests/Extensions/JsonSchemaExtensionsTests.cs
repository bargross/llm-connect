using FluentAssertions;
using LLMConnect.Models;

namespace LLMConnect.Tests.MappingExtensions;

public class JsonSchemaExtensionsTests
{
    [Fact]
    public void ToDictionary_WithOnlyType_ReturnsMinimalDictionary()
    {
        // Arrange
        var schema = new JsonSchema { Type = "string" };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("type").WhoseValue.Should().Be("string");
        result.Should().NotContainKey("description");
        result.Should().NotContainKey("properties");
        result.Should().NotContainKey("items");
        result.Should().NotContainKey("enum");
        result.Should().NotContainKey("extra");
    }

    [Fact]
    public void ToDictionary_WithDescription_AddsDescription()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "string",
            Description = "A string field"
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("description").WhoseValue.Should().Be("A string field");
    }

    [Fact]
    public void ToDictionary_WithProperties_AddsNestedProperties()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchema { Type = "string" },
                ["age"] = new JsonSchema { Type = "integer" }
            }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("properties");
        var props = result["properties"] as Dictionary<string, object>;
        props.Should().ContainKey("name");
        var nameSchema = props["name"] as Dictionary<string, object>;
        nameSchema.Should().ContainKey("type").WhoseValue.Should().Be("string");
        props.Should().ContainKey("age");
        var ageSchema = props["age"] as Dictionary<string, object>;
        ageSchema.Should().ContainKey("type").WhoseValue.Should().Be("integer");
    }

    [Fact]
    public void ToDictionary_WithItems_AddsItems()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "array",
            Items = new JsonSchema { Type = "string" }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("items");
        var items = result["items"] as Dictionary<string, object>;
        items.Should().ContainKey("type").WhoseValue.Should().Be("string");
    }

    [Fact]
    public void ToDictionary_WithEnum_AddsEnum()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "string",
            Enum = new List<object> { "red", "green", "blue" }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("enum");
        var enumList = result["enum"] as List<object>;
        enumList.Should().BeEquivalentTo(new object[] { "red", "green", "blue" });
    }

    [Fact]
    public void ToDictionary_WithExtra_AddsExtraFields()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Extra = new Dictionary<string, object>
            {
                ["x-custom"] = "value",
                ["nullable"] = true
            }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("x-custom").WhoseValue.Should().Be("value");
        result.Should().ContainKey("nullable").WhoseValue.Should().Be(true);
    }

    [Fact]
    public void ToDictionary_WithAllProperties_ReturnsFullDictionary()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Description = "A person",
            Properties = new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchema { Type = "string" }
            },
            Items = new JsonSchema { Type = "string" },
            Enum = new List<object> { "one", "two" },
            Extra = new Dictionary<string, object> { ["x-extra"] = 123 }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().ContainKey("type");
        result.Should().ContainKey("description");
        result.Should().ContainKey("properties");
        result.Should().ContainKey("items");
        result.Should().ContainKey("enum");
        result.Should().ContainKey("x-extra");
    }

    [Fact]
    public void ToDictionary_WhenPropertiesIsEmpty_DoesNotAddProperties()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, JsonSchema>()
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().NotContainKey("properties");
    }

    [Fact]
    public void ToDictionary_WhenEnumIsEmpty_DoesNotAddEnum()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "string",
            Enum = new List<object>()
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().NotContainKey("enum");
    }

    [Fact]
    public void ToDictionary_WhenExtraIsEmpty_DoesNotAddExtra()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Extra = new Dictionary<string, object>()
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().NotContainKey("extra");
        // Any extra fields should not be present (they are not added since Extra is empty).
    }

    [Fact]
    public void ToDictionary_WhenItemsIsNull_DoesNotAddItems()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "array",
            Items = null
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        result.Should().NotContainKey("items");
    }

    [Fact]
    public void ToDictionary_NestedProperties_RecursivelyTransforms()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, JsonSchema>
            {
                ["address"] = new JsonSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["street"] = new JsonSchema { Type = "string" }
                    }
                }
            }
        };

        // Act
        var result = schema.ToDictionary();

        // Assert
        var props = result["properties"] as Dictionary<string, object>;
        var address = props["address"] as Dictionary<string, object>;
        address.Should().ContainKey("properties");
        var addressProps = address["properties"] as Dictionary<string, object>;
        addressProps.Should().ContainKey("street");
        var street = addressProps["street"] as Dictionary<string, object>;
        street.Should().ContainKey("type").WhoseValue.Should().Be("string");
    }
}