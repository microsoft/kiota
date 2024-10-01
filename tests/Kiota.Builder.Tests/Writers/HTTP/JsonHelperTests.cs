using Kiota.Builder.Writers.http;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.http;
public class JsonHelperTests
{
    [Fact]
    public void StripJsonDownToRequestObject_ExampleAtTopLevel_ReturnsExample()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'example': {
                    'field': 'value'
                }
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("value", result["field"].ToString());
    }

    [Fact]
    public void StripJsonDownToRequestObject_OneOf_ReturnsFirstSchema()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'oneOf': [
                    { 'type': 'object', 'properties': { 'field1': { 'type': 'string' } } },
                    { 'type': 'object', 'properties': { 'field2': { 'type': 'integer' } } }
                ]
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["field1"].ToString());
    }

    [Fact]
    public void StripJsonDownToRequestObject_AllOf_MergesSchemas()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'allOf': [
                    { 'type': 'object', 'properties': { 'field1': { 'type': 'string' } } },
                    { 'type': 'object', 'properties': { 'field2': { 'type': 'integer' } } }
                ]
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["field1"].ToString());
        Assert.Equal(0, result["field2"].ToObject<int>());
    }

    [Fact]
    public void StripJsonDownToRequestObject_Enum_UsesFirstEnumValue()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'type': 'object',
                'properties': {
                    'field': {
                        'type': 'string',
                        'enum': ['First', 'Second']
                    }
                }
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("First", result["field"].ToString());
    }

    [Fact]
    public void StripJsonDownToRequestObject_Array_UsesEmptyArrayPlaceholder()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'type': 'object',
                'properties': {
                    'field': {
                        'type': 'array',
                        'items': {
                            'type': 'string'
                        }
                    }
                }
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.True(result["field"] is JArray);
    }

    [Fact]
    public void StripJsonDownToRequestObject_ObjectProperties_ProcessedCorrectly()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'type': 'object',
                'properties': {
                    'field1': { 'type': 'string' },
                    'field2': { 'type': 'integer' }
                }
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["field1"].ToString());
        Assert.Equal(0, result["field2"].ToObject<int>());
    }

    [Fact]
    public void StripJsonDownToRequestObject_OneOf_WithComplexObject_ReturnsFirstObject()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'oneOf': [
                    { 'type': 'object', 'properties': { 'field1': { 'type': 'string' } } },
                    { 'type': 'object', 'properties': { 'field2': { 'type': 'integer' } } }
                ]
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["field1"].ToString());
    }

    [Fact]
    public void StripJsonDownToRequestObject_AllOf_WithComplexObject_MergesObjects()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'allOf': [
                    { 'type': 'object', 'properties': { 'field1': { 'type': 'string' } } },
                    { 'type': 'object', 'properties': { 'field2': { 'type': 'boolean' } } }
                ]
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["field1"].ToString());
        Assert.True(result["field2"].ToObject<bool>());
    }

    [Fact]
    public void StripJsonDownToRequestObject_PrimitiveTypes_ProcessedCorrectly()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'type': 'object',
                'properties': {
                    'stringField': { 'type': 'string' },
                    'integerField': { 'type': 'integer' },
                    'numberField': { 'type': 'number' },
                    'booleanField': { 'type': 'boolean' },
                    'nullField': { 'type': 'null' }
                }
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal("string", result["stringField"].ToString());
        Assert.Equal(0, result["integerField"].ToObject<int>());
        Assert.Equal(0.0, result["numberField"].ToObject<double>());
        Assert.True(result["booleanField"].ToObject<bool>());
    }

    [Fact]
    public void StripJsonDownToRequestObject_NoProperties_ReturnsInput()
    {
        // Arrange
        var json = JObject.Parse(@"{
                'type': 'object'
            }");

        // Act
        var result = JsonHelper.StripJsonDownToRequestObject(json);

        // Assert
        Assert.Equal(json, result);
    }
}
