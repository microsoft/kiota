using System.IO;
using System.Text.Json.Nodes;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public sealed class OpenApiLogoExtensionTest
{
    [Fact]
    public void Parses()
    {
        var oaiValueRepresentation =
        """
        {
            "url": "https://example.com/logo.png"
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        var oaiValue = JsonNode.Parse(stream);
        var value = OpenApiLogoExtension.Parse(oaiValue);
        
        Assert.NotNull(value);
        Assert.Equal("https://example.com/logo.png", value.Url);
    }

    [Fact]
    public void Serializes()
    {
        var value = new OpenApiLogoExtension
        {
            Url = "https://example.com/logo.png"
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });

        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        
        Assert.Equal("{\"url\":\"https://example.com/logo.png\"}", result);
    }

    [Fact]
    public void WritesNothingForEmptyLogo()
    {
        var value = new OpenApiLogoExtension
        {
            Url = null
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });

        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();

        // When Url is null/empty, nothing should be written
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WritesNothingForEmptyUrlString()
    {
        var value = new OpenApiLogoExtension
        {
            Url = string.Empty
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });

        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();

        // When Url is empty string, nothing should be written
        Assert.Equal(string.Empty, result);
    }
}