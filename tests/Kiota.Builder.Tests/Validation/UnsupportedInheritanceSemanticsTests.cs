using System.IO;
using System.Text;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Xunit;

namespace Kiota.Builder.Tests.Validation;
public class UnsupportedInheritanceSemanticsTests
{
    [Fact]
    public void DoesntAddAWarningWhenBodyIsSimple()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
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
    public void AddsWarningOnUnsupportedInheritance()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /api/v1/users:
    get:
      summary: Retrieves a list of users
      description: 'Returns a list of all users'
      responses:
        '200':
          description: List of users
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserList'
components:
  schemas:
    List:
      required:
      - total
      - items
      type: object
      properties:
        items:
          type: array
          items:
            type: object
        total:
          format: int32
          description: Total number of entries in the full result set
          type: integer
          nullable: false
    UserList:
      allOf:
      - $ref: '#/components/schemas/List'
      - description: List of users
        type: object
        properties:
          items:
            type: array
            items:
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
    public void DoNotAddsWarningOnUnsupportedInheritanceIfNotNeeded()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /api/v1/users:
    get:
      summary: Retrieves a list of users
      description: 'Returns a list of all users'
      responses:
        '200':
          description: List of users
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserList'
components:
  schemas:
    List:
      required:
      - total
      - items
      type: object
      properties:
        items:
          type: array
          items:
            type: object
        total:
          format: int32
          description: Total number of entries in the full result set
          type: integer
          nullable: false
    UserList:
      allOf:
      - $ref: '#/components/schemas/List'
      - description: List of users
        type: object
        properties:
          items2:
            type: array
            items:
              type: string";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
    [Fact]
    public void AddsWarningOnUnsupportedInheritanceInComponents()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /api/v1/users:
    get:
      summary: Retrieves a list of users
      description: 'Returns a list of all users'
      responses:
        '200':
          description: List of users
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserList'
components:
  schemas:
    List:
      required:
      - total
      - items
      type: object
      properties:
        items:
          type: array
          items:
            type: object
        total:
          format: int32
          description: Total number of entries in the full result set
          type: integer
          nullable: false
    Example:
      required:
      - items
      type: object
      properties:
        items:
          type: array
          items:
            type: string
    UserList:
      allOf:
      - $ref: '#/components/schemas/List'
      - $ref: '#/components/schemas/Example'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Single(diag.Warnings);
    }
    [Fact]
    public void AddsWarningOnUnsupportedInheritanceInNestedComponents()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /api/v1/users:
    get:
      summary: Retrieves a list of users
      description: 'Returns a list of all users'
      responses:
        '200':
          description: List of users
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserList'
components:
  schemas:
    List:
      required:
      - total
      - items
      type: object
      properties:
        items:
          allOf:
            - $ref: '#/components/schemas/Example'
          type: object
          properties:
            x:
              type: string
        total:
          format: int32
          description: Total number of entries in the full result set
          type: integer
          nullable: false
    Example:
      required:
      - x
      type: object
      properties:
        x:
          type: array
          items:
            type: object
    UserList:
      allOf:
      - $ref: '#/components/schemas/List'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Equal(2, diag.Warnings.Count);
    }
    [Fact]
    public void DoNotAddsWarningOnDefaultValueOverloading()
    {
        var rule = new UnsupportedInheritanceSemantics(new());
        var documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /api/v1/users:
    get:
      summary: Retrieves a list of users
      description: 'Returns a list of all users'
      responses:
        '200':
          description: List of users
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UserList'
components:
  schemas:
    List:
      required:
      - total
      - items
      type: object
      properties:
        items:
          allOf:
            - $ref: '#/components/schemas/Example'
          type: object
          properties:
            x:
              type: string
              default: 'foo'
        total:
          format: int32
          description: Total number of entries in the full result set
          type: integer
          nullable: false
    Example:
      type: object
      properties:
        x:
          type: string
          default: 'bar'
    UserList:
      allOf:
      - $ref: '#/components/schemas/List'";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
}
