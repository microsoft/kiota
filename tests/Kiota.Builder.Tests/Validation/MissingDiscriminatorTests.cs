using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class MissingDiscriminatorTests
{
    public static TheoryData<string, bool, bool, bool> NullableModelUnions
    {
        get
        {
            var data = new TheoryData<string, bool, bool, bool>();
            foreach (var unionKind in new[] { "anyOf", "oneOf" })
                foreach (var nullFirst in new[] { false, true })
                    foreach (var includeNullable in new[] { false, true })
                        foreach (var componentSchema in new[] { false, true })
                            data.Add(unionKind, nullFirst, includeNullable, componentSchema);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(NullableModelUnions))]
    public async Task DoesntAddWarningForNullableSingleModel(string unionKind, bool nullFirst, bool includeNullable, bool componentSchema)
    {
        var modelBranch = "- $ref: '#/components/schemas/Preview'";
        var nullBranch = includeNullable ? "- enum: [null]\n  nullable: true" : "- enum: [null]";
        var branches = nullFirst ? $"{nullBranch}\n{modelBranch}" : $"{modelBranch}\n{nullBranch}";
        var diagnostic = await GetDiagnosticFromDocumentAsync(CreateUnionDocument(unionKind, branches, componentSchema));
        Assert.Empty(diagnostic.Errors);
        Assert.Empty(diagnostic.Warnings);
    }

    [Theory]
    [InlineData("anyOf", false)]
    [InlineData("anyOf", true)]
    [InlineData("oneOf", false)]
    [InlineData("oneOf", true)]
    public async Task AddsWarningForMultipleModelsAndNull(string unionKind, bool componentSchema)
    {
        var branches = """
            - $ref: '#/components/schemas/Preview'
            - enum: [null]
            - $ref: '#/components/schemas/OtherPreview'
            """;
        var diagnostic = await GetDiagnosticFromDocumentAsync(CreateUnionDocument(unionKind, branches, componentSchema));
        Assert.Empty(diagnostic.Errors);
        Assert.Contains("is a polymorphic type", Assert.Single(diagnostic.Warnings).Message);
    }

    [Theory]
    [InlineData("anyOf", "null, other")]
    [InlineData("oneOf", "null, other")]
    [InlineData("anyOf", "'null'")]
    [InlineData("oneOf", "'null'")]
    public async Task AddsWarningWhenEnumAllowsNonNullValues(string unionKind, string enumValues)
    {
        var branches = $$"""
            - $ref: '#/components/schemas/Preview'
            - enum: [{{enumValues}}]
            """;
        var diagnostic = await GetDiagnosticFromDocumentAsync(CreateUnionDocument(unionKind, branches, false));
        Assert.Empty(diagnostic.Errors);
        Assert.Contains("is a polymorphic type", Assert.Single(diagnostic.Warnings).Message);
    }

    [Theory]
    [InlineData("anyOf")]
    [InlineData("oneOf")]
    public async Task DoesntAddWarningForModelWithNullType(string unionKind)
    {
        var branches = """
            - $ref: '#/components/schemas/Preview'
            - type: 'null'
            """;
        var document = CreateUnionDocument(unionKind, branches, false).Replace("openapi: 3.0.4", "openapi: 3.1.0");
        var diagnostic = await GetDiagnosticFromDocumentAsync(document);
        Assert.Empty(diagnostic.Errors);
        Assert.Empty(diagnostic.Warnings);
    }

    private static string CreateUnionDocument(string unionKind, string branches, bool componentSchema)
    {
        var unionSchema = $"{unionKind}:\n  {branches.Replace("\n", "\n  ")}";
        var responseSchema = componentSchema ? "$ref: '#/components/schemas/NullablePreview'" : unionSchema;
        var component = componentSchema ? $"\n    NullablePreview:\n      {unionSchema.Replace("\n", "\n      ")}" : string.Empty;
        return $$"""
            openapi: 3.0.4
            info:
              title: Nullable object response
              version: 1.0.0
            paths:
              /preview:
                get:
                  responses:
                    '200':
                      description: Nullable preview
                      content:
                        application/json:
                          schema:
                            {{responseSchema.Replace("\n", "\n                ")}}
            components:
              schemas:
                Preview:
                  type: object
                  properties:
                    previewText:
                      type: string
                OtherPreview:
                  type: object
                  properties:
                    otherText:
                      type: string{{component}}
            """;
    }

    [Fact]
    public async Task DoesntAddAWarningWhenBodyIsSimple()
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
    public async Task AddsWarningOnInlineSchemas()
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
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task AddsWarningOnComponentSchemas()
    {
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
          description: some description
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type3'";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddsWarningOnComponentSchemasWithDiscriminatorInformation()
    {
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
          description: some description
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type3'";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    [Fact]
    public async Task DoesntAddsWarningOnComponentSchemasScalars()
    {
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
          description: some description
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/type1'";
        var diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic.Warnings);
    }
    private static async Task<OpenApiDiagnostic> GetDiagnosticFromDocumentAsync(string document)
    {
        var rule = new MissingDiscriminator(new());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document));
        var settings = new OpenApiReaderSettings();
        settings.RuleSet.Add(typeof(OpenApiDocument), [rule]);
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
