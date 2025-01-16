using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Settings.Tests
{
    public class SettingsFileManagementServiceTest
    {
        [Fact]
        public void GetDirectoryContainingSettingsFile_ShouldReturnNull_WhenNoVscodeDirectoryExists()
        {
            // Arrange
            var service = new SettingsFileManagementService();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            // Act
            var result = service.GetDirectoryContainingSettingsFile(tempDirectory);

            // Assert
            Assert.Null(result);

            // Cleanup
            Directory.Delete(tempDirectory);
        }

        [Fact]
        public void GetDirectoryContainingSettingsFile_ShouldReturnVscodeDirectory_WhenExists()
        {
            // Arrange
            var service = new SettingsFileManagementService();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var vscodeDirectory = Path.Combine(tempDirectory, ".vscode");
            Directory.CreateDirectory(vscodeDirectory);

            // Act
            var result = service.GetDirectoryContainingSettingsFile(tempDirectory);

            // Assert
            Assert.Equal(vscodeDirectory, result);

            // Cleanup
            Directory.Delete(tempDirectory, true);
        }

        [Fact]
        public async Task WriteSettingsFileAsync_ShouldThrowArgumentException_WhenDirectoryPathIsNullOrEmpty()
        {
            // Arrange
            var service = new SettingsFileManagementService();
            var openApiDocument = new OpenApiDocument();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.WriteSettingsFileAsync(string.Empty, openApiDocument, cancellationToken));
        }

        [Fact]
        public async Task WriteSettingsFileAsync_ShouldThrowArgumentNullException_WhenOpenApiDocumentIsNull()
        {
            // Arrange
            var service = new SettingsFileManagementService();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.WriteSettingsFileAsync(tempDirectory, null, cancellationToken));

            // Cleanup
            Directory.Delete(tempDirectory);
        }
    }
}
