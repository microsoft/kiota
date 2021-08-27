using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Kiota.Serialization.Json.Tests.Mocks;
using Xunit;

namespace Microsoft.Kiota.Serialization.Json.Tests
{
    public class JsonSerializationWriterTests
    {
        [Fact]
        public void WritesSampleObjectValue()
        {
            // Arrange
            var testEntity = new TestEntity()
            {
                Id = "48d31887-5fad-4d73-a9f5-3c356e68a038",
                AdditionalData = new Dictionary<string, object>
                {
                    {"mobilePhone",null}, // write null value
                    {"accountEnabled",false}, // write bool value
                    {"jobTitle","Author"}, // write string value
                    {"createdDateTime", DateTimeOffset.MinValue}, // write date value
                    {"businessPhones", new List<string>() {"+1 412 555 0109"}}, // write collection of primitives value
                    {"manager", new TestEntity{Id = "48d31887-5fad-4d73-a9f5-3c356e68a038"}}, // write nested object value
                }
            };
            using var jsonSerializerWriter = new JsonSerializationWriter();
            // Act
            jsonSerializerWriter.WriteObjectValue(string.Empty,testEntity);
            // Get the json string from the stream.
            var serializedStream = jsonSerializerWriter.GetSerializedContent();
            using var reader = new StreamReader(serializedStream, Encoding.UTF8);
            var serializedJsonString = reader.ReadToEnd();
            
            // Assert
            var expectedString = "{" +
                                 "\"id\":\"48d31887-5fad-4d73-a9f5-3c356e68a038\"," +
                                 "\"mobilePhone\":null," +
                                 "\"accountEnabled\":false," +
                                 "\"jobTitle\":\"Author\"," +
                                 "\"createdDateTime\":\"0001-01-01T00:00:00+00:00\"," +
                                 "\"businessPhones\":[\"\\u002B1 412 555 0109\"]," +
                                 "\"manager\":{\"id\":\"48d31887-5fad-4d73-a9f5-3c356e68a038\"}"+
                                 "}";
            Assert.Equal(expectedString, serializedJsonString);
        }

        [Fact]
        public void WritesSampleCollectionOfObjectValues()
        {
            // Arrange
            var testEntity = new TestEntity()
            {
                Id = "48d31887-5fad-4d73-a9f5-3c356e68a038",
                AdditionalData = new Dictionary<string, object>
                {
                    {"mobilePhone",null}, // write null value
                    {"accountEnabled",false}, // write bool value
                    {"jobTitle","Author"}, // write string value
                    {"createdDateTime", DateTimeOffset.MinValue}, // write date value
                    {"businessPhones", new List<string>() {"+1 412 555 0109"}}, // write collection of primitives value
                    {"manager", new TestEntity{Id = "48d31887-5fad-4d73-a9f5-3c356e68a038"}}, // write nested object value
                }
            };
            var entityList = new List<TestEntity>() { testEntity};
            using var jsonSerializerWriter = new JsonSerializationWriter();
            // Act
            jsonSerializerWriter.WriteCollectionOfObjectValues(string.Empty, entityList);
            // Get the json string from the stream.
            var serializedStream = jsonSerializerWriter.GetSerializedContent();
            using var reader = new StreamReader(serializedStream, Encoding.UTF8);
            var serializedJsonString = reader.ReadToEnd();

            // Assert
            var expectedString = "[{" +
                                 "\"id\":\"48d31887-5fad-4d73-a9f5-3c356e68a038\"," +
                                 "\"mobilePhone\":null," +
                                 "\"accountEnabled\":false," +
                                 "\"jobTitle\":\"Author\"," +
                                 "\"createdDateTime\":\"0001-01-01T00:00:00+00:00\"," +
                                 "\"businessPhones\":[\"\\u002B1 412 555 0109\"]," +
                                 "\"manager\":{\"id\":\"48d31887-5fad-4d73-a9f5-3c356e68a038\"}" +
                                 "}]";
            Assert.Equal(expectedString, serializedJsonString);
        }

    }
}
