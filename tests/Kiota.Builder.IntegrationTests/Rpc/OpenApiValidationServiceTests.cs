using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Validation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kiota.Builder.IntegrationTests.Rpc
{
    public class OpenApiValidationServiceTests
    {
        [Fact]
        public async Task TestGetDocumentAsync()
        {
            // Arrange
            var httpClient = new HttpClient();
            ILoggerFactory nullFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
            var logger = nullFactory.CreateLogger<OpenApiValidationService>();
            var openApiValidationService = new OpenApiValidationService(httpClient, logger);
            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "ModelWithDerivedTypes.yaml");

            var cancellationToken = new CancellationToken();
            // Act
            var result = await openApiValidationService.GetDocumentAsync(inputPath, cancellationToken);
            // Assert
            Assert.NotNull(result);

            httpClient.Dispose();
        }

        [Fact]
        public async Task TestGetAndValidateDocumentAsync_InvalidDocument()
        {
            // Arrange
            var httpClient = new HttpClient();
            ILoggerFactory nullFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
            var logger = nullFactory.CreateLogger<OpenApiValidationService>();
            var openApiValidationService = new OpenApiValidationService(httpClient, logger);
            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "DiscriminatorSampleWithErrors.yaml");

            var cancellationToken = new CancellationToken();
            // Act
            var result = await openApiValidationService.GetDocumentAsync(inputPath, cancellationToken);
            // Assert
            Assert.NotNull(result);

            httpClient.Dispose();
        }

        [Fact]
        public async Task TestGetAndValidateDocumentAsync_RemoteLocation()
        {
            
            // Arrange
            var httpClient = new HttpClient();
            ILoggerFactory nullFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
            var logger = nullFactory.CreateLogger<OpenApiValidationService>();
            var openApiValidationService = new OpenApiValidationService(httpClient, logger);
            var inputPath = "https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml";

            var cancellationToken = new CancellationToken();
            // Act
            var result = await openApiValidationService.GetDocumentAsync(inputPath, cancellationToken);
            // Assert
            Assert.NotNull(result);

            httpClient.Dispose();
        }
    }
}
