using System;
using System.IO;
using Kiota.Builder.Configuration;
using Kiota.Builder.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Plugins
{
    public class PluginsGenerationServiceHelpersTests
    {
        private readonly ILogger<KiotaBuilder> _logger = new Mock<ILogger<KiotaBuilder>>().Object;

        private PluginsGenerationService CreatePluginsGenerationService(GenerationConfiguration generationConfiguration)
        {
            var openApiDocument = new OpenApiDocument();
            var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);
            var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        }

        [Fact]
        public void SanitizeClientClassName_RemovesSpecialCharacters()
        {
            // Arrange
            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = "My@Client#Name!"
            });

            // Act
            var result = pluginsGenerationService.SanitizeClientClassName();

            // Assert
            Assert.Equal("MyClientName", result);
        }

        [Fact]
        public void SanitizeClientClassName_LeavesAlphanumericCharacters()
        {
            // Arrange
            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = "Client123Name"
            });

            // Act
            var result = pluginsGenerationService.SanitizeClientClassName();

            // Assert
            Assert.Equal("Client123Name", result);
        }

        [Fact]
        public void SanitizeClientClassName_ReturnsEmptyString_WhenInputIsEmpty()
        {
            // Arrange
            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = string.Empty
            });

            // Act
            var result = pluginsGenerationService.SanitizeClientClassName();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SanitizeClientClassName_ThrowsArgumentNullException_WhenInputIsNull()
        {
            // Arrange
            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = null
            });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pluginsGenerationService.SanitizeClientClassName());
        }

        [Fact]
        public void EnsureOutputDirectoryExists_CreatesDirectory_WhenItDoesNotExist()
        {
            // Arrange
            var tempDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
            var tempFilePath = Path.Combine(tempDirectory, "testfile.txt");
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = "Client123Name"
            });

            // Act
            pluginsGenerationService.EnsureOutputDirectoryExists(tempFilePath);

            // Assert
            Assert.True(Directory.Exists(tempDirectory));

            // Cleanup
            Directory.Delete(tempDirectory, true);
        }

        [Fact]
        public void EnsureOutputDirectoryExists_DoesNothing_WhenDirectoryExists()
        {
            // Arrange
            var tempDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
            Directory.CreateDirectory(tempDirectory);
            var tempFilePath = Path.Combine(tempDirectory, "testfile.txt");

            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = "Client123Name"
            });

            // Act
            pluginsGenerationService.EnsureOutputDirectoryExists(tempFilePath);

            // Assert
            Assert.True(Directory.Exists(tempDirectory));

            // Cleanup
            Directory.Delete(tempDirectory, true);
        }

        [Theory]
        [InlineData("ClientName", "description_m1-2.yaml", 2, "ClientName-2", "description_m2-2.yaml")]
        [InlineData("ClientName", "description_m1-3.yaml", 2, "ClientName-2", "description_m2-3.yaml")]
        [InlineData("ClientName", "description_m1-3.yaml", 3, "ClientName-3", "description_m3-3.yaml")]
        [InlineData("ClientName", "description_m1-9.yaml", 5, "ClientName-5", "description_m5-9.yaml")]
        [InlineData("MyClient",   "description_m1-8.yaml", 3, "MyClient-3",   "description_m3-8.yaml")]
        [InlineData("MyClient",   "description_m1-8.yaml", 8, "MyClient-8",   "description_m8-8.yaml")]
        public void GetNextFileName_ValidInputs_ReturnsExpectedResults(string originalClientClassName, String originalFilePath, int fileNumber, string expectedClientClassName, string expectedFilePath)
        {
            // Arrange
            var pluginsGenerationService = CreatePluginsGenerationService(new GenerationConfiguration
            {
                ClientClassName = originalClientClassName,
                OpenAPIFilePath = originalFilePath
            });

            // Act
            var (updatedClientClassName, updatedOpenAPIFilePath) = pluginsGenerationService.GetNextFileInfo(originalClientClassName, originalFilePath, fileNumber);

            // Assert
            Assert.Equal(expectedClientClassName, updatedClientClassName);
            Assert.Equal(expectedFilePath, updatedOpenAPIFilePath);
        }

        [Fact]
        public void GetNextFileName_InvalidRegex_ThrowsException()
        {
            // Arrange
            var generationConfiguration = new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                OpenAPIFilePath = "invalid_file_name.yaml"
            };
            var pluginsGenerationService = CreatePluginsGenerationService(generationConfiguration);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => pluginsGenerationService.GetNextFileInfo(generationConfiguration.ClientNamespaceName, generationConfiguration.OpenAPIFilePath, 2));
        }


    }

}
