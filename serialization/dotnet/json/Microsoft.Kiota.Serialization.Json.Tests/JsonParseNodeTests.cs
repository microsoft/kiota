using System.Linq;
using System.Text.Json;
using Microsoft.Kiota.Serialization.Json.Tests.Mocks;
using Xunit;

namespace Microsoft.Kiota.Serialization.Json.Tests
{
    public class JsonParseNodeTests
    {
        private const string TestUserJson = "{\r\n" +
                                            "    \"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users/$entity\",\r\n" +
                                            "    \"@odata.id\": \"https://graph.microsoft.com/v2/dcd219dd-bc68-4b9b-bf0b-4a33a796be35/directoryObjects/48d31887-5fad-4d73-a9f5-3c356e68a038/Microsoft.DirectoryServices.User\",\r\n" +
                                            "    \"businessPhones\": [\r\n" +
                                            "        \"+1 412 555 0109\"\r\n" +
                                            "    ],\r\n" +
                                            "    \"displayName\": \"Megan Bowen\",\r\n" +
                                            "    \"givenName\": \"Megan\",\r\n" +
                                            "    \"accountEnabled\": true,\r\n" +
                                            "    \"createdDateTime\": \"2017 -07-29T03:07:25Z\",\r\n" +
                                            "    \"jobTitle\": \"Auditor\",\r\n" +
                                            "    \"mail\": \"MeganB@M365x214355.onmicrosoft.com\",\r\n" +
                                            "    \"mobilePhone\": null,\r\n" +
                                            "    \"officeLocation\": \"12/1110\",\r\n" +
                                            "    \"preferredLanguage\": \"en-US\",\r\n" +
                                            "    \"surname\": \"Bowen\",\r\n" +
                                            "    \"userPrincipalName\": \"MeganB@M365x214355.onmicrosoft.com\",\r\n" +
                                            "    \"id\": \"48d31887-5fad-4d73-a9f5-3c356e68a038\"\r\n" +
                                            "}";

        private static readonly string TestUserCollectionString = $"[{TestUserJson}]";

        [Fact]
        public void GetsEntityValueFromJson()
        {
            // Arrange
            using var jsonDocument = JsonDocument.Parse(TestUserJson);
            var jsonParseNode = new JsonParseNode(jsonDocument.RootElement);
            // Act
            var testEntity = jsonParseNode.GetObjectValue<TestEntity>();
            // Assert
            Assert.NotNull(testEntity);
            Assert.NotEmpty(testEntity.AdditionalData);
            Assert.True(testEntity.AdditionalData.ContainsKey("jobTitle"));
            Assert.True(testEntity.AdditionalData.ContainsKey("mobilePhone"));
            Assert.Equal("Auditor", testEntity.AdditionalData["jobTitle"]);
            Assert.Equal("48d31887-5fad-4d73-a9f5-3c356e68a038", testEntity.Id);
        }

        [Fact]
        public void GetCollectionOfObjectValuesFromJson()
        {
            // Arrange
            using var jsonDocument = JsonDocument.Parse(TestUserCollectionString);
            var jsonParseNode = new JsonParseNode(jsonDocument.RootElement);
            // Act
            var testEntityCollection = jsonParseNode.GetCollectionOfObjectValues<TestEntity>().ToArray();
            // Assert
            Assert.NotEmpty(testEntityCollection);
            Assert.Equal("48d31887-5fad-4d73-a9f5-3c356e68a038", testEntityCollection[0].Id);
        }

        [Fact]
        public void GetsChildNodeAndGetCollectionOfPrimitiveValuesFromJsonParseNode()
        {
            // Arrange
            using var jsonDocument = JsonDocument.Parse(TestUserJson);
            var rootParseNode = new JsonParseNode(jsonDocument.RootElement);
            // Act to get business phones list
            var phonesListChildNode = rootParseNode.GetChildNode("businessPhones");
            var phonesList = phonesListChildNode.GetCollectionOfPrimitiveValues<string>().ToArray();
            // Assert
            Assert.NotEmpty(phonesList);
            Assert.Equal("+1 412 555 0109", phonesList[0]);
        }
    }
}
