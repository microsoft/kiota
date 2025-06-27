using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class UrlFormEncodedComplexTests
{
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedNotObjectRequestBody()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      requestBody:
        content:
          application/x-www-form-urlencoded:
            schema:
              type: string
              format: int32
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedNotObjectResponse()
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
            application/x-www-form-urlencoded:
              schema:
                type: string
                format: int32";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedComplexPropertyOnRequestBody()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      requestBody:
        content:
          application/x-www-form-urlencoded:
            schema:
              type: object
              properties:
                complex:
                  type: object
                  properties:
                    prop:
                      type: string
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedComplexPropertyOnResponse()
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
            application/x-www-form-urlencoded:
              schema:
                  type: object
                  properties:
                    complex:
                      type: object
                      properties:
                        prop:
                          type: string";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenUrlEncoded()
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
            application/x-www-form-urlencoded:
              schema:
                type: object
                properties:
                  prop:
                    type: string";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningOnArrayProperty()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    post:
      requestBody:
        content:
          application/x-www-form-urlencoded:
            schema:
              type: object
              properties:
                filters:
                  type: array
                  items:
                    type: integer
                    format: int32
      responses:
        '200':
          description: some description
          content:
            application/x-www-form-urlencoded:
              schema:
                type: object
                properties:
                  prop:
                    type: string";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenNotUrlEncoded()
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
                type: enum
                format: string";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    private static async Task<OpenApiDiagnostic> GetDiagnosticFromDocumentAsync(string document)
    {
        var rule = new UrlFormEncodedComplex();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document));
        var settings = new OpenApiReaderSettings();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
