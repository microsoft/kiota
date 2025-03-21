using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class DivergentResponseSchemaTests
{
    [Fact]
    public async Task DoesntAddAWarningWhenBodyIsSingle()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenBodyIsDivergent()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32
        '201':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int64";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenUsing2XX()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32
        '2XX':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int64";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    private static async Task<OpenApiDiagnostic> GetDiagnosticFromDocumentAsync(string document)
    {
        var rule = new DivergentResponseSchema(new());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document));
        var settings = new OpenApiReaderSettings();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
