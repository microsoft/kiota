﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class NoContentWithBodyTests
{
    [Fact]
    public async Task AddsAWarningWhen204WithBody()
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
        '204':
          description: some description
          content:
            application/json:";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhen204WithNoBody()
    {
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '204':
          description: some description";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhen200WithBody()
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
        description: New navigation property
        content:
          application/json:
      responses:
        '200':
          description: some description
          content:
            application/json:";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    private static async Task<OpenApiDiagnostic> GetDiagnosticFromDocumentAsync(string document)
    {
        var rule = new NoContentWithBody();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document));
        var settings = new OpenApiReaderSettings();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        OpenApiReaderRegistry.RegisterReader(OpenApiConstants.Yaml, new OpenApiYamlReader());
        OpenApiReaderRegistry.RegisterReader(OpenApiConstants.Yml, new OpenApiYamlReader());
        var result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
