using System.IO;
using System.Text;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Xunit;

namespace Kiota.Builder.Tests.Validation;
public class MissingDiscriminatorTests
{
    [Fact]
    public void DoesntAddAWarningWhenBodyIsSimple()
    {
        var rule = new MissingDiscriminator(new());
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
          content:
            application/json:
              schema:
                type: string
                format: int32";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
    [Fact]
    public void AddsWarningOnInlineSchemas()
    {
        var rule = new MissingDiscriminator(new());
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
          content:
            application/json:
              schema:
                type: object
                oneOf:
                  - type: object
                    properties:
                      type:
                        type: string
                  - type: object
                    properties:
                      type2:
                        type: string";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Single(diag.Warnings);
    }
    [Fact]
    public void AddsWarningOnComponentSchemas()
    {
        var rule = new MissingDiscriminator(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
components:
    schemas:
      type1:
        type: object
        properties:
          type:
            type: string
      type2:
        type: object
        properties:
          type2:
            type: string
      type3:
        type: object
        oneOf:
          - $ref: '#/components/schemas/type1'
          - $ref: '#/components/schemas/type2'
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type3'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Single(diag.Warnings);
    }
    [Fact]
    public void DoesntAddsWarningOnComponentSchemasWithDiscriminatorInformation()
    {
        var rule = new MissingDiscriminator(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
components:
    schemas:
      type1:
        type: object
        properties:
          type:
            type: string
      type2:
        type: object
        properties:
          type2:
            type: string
      type3:
        type: object
        oneOf:
          - $ref: '#/components/schemas/type1'
          - $ref: '#/components/schemas/type2'
        discriminator:
          propertyName: type
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type3'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
    [Fact]
    public void DoesntAddsWarningOnComponentSchemasScalars()
    {
        var rule = new MissingDiscriminator(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
components:
    schemas:
      type1:
        type: object
        oneOf:
          - type: string
          - type: number
        discriminator:
          propertyName: type
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type1'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
}
