using System.IO;
using System.Text;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Xunit;

namespace Kiota.Builder.Tests.Validation;
public class InconsistentTypeFormatPairTests
{
    [Fact]
    public void AddsAWarningWhenKnownInconsistentPair()
    {
        var rule = new InconsistentTypeFormatPair();
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
        Assert.Single(diag.Warnings);
    }
    [Fact]
    public void DoesntAddAWarningWhenSupportedPair()
    {
        var rule = new InconsistentTypeFormatPair();
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
                format: uuid";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
    [Fact]
    public void DoesntFailWhenKnownAlternative()
    {
        var rule = new InconsistentTypeFormatPair();
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
                type: enum
                format: string";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentTxt));
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            RuleSet = new(new ValidationRule[] { rule }),
        });
        var doc = reader.Read(stream, out var diag);
        Assert.Empty(diag.Warnings);
    }
}
