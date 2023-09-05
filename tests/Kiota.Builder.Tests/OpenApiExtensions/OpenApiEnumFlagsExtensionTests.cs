using System.Collections.Generic;
using System.IO;
using Kiota.Builder.OpenApiExtensions;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Writers;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public class OpenApiEnumFlagsExtensionTests
{
    [Fact]
    public void ExtensionNameMatchesExpected()
    {
        // Act
        string name = OpenApiEnumFlagsExtension.Name;
        string expectedName = "x-ms-enum-flags";

        // Assert
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void WritesDefaultValues()
    {
        // Arrange
        OpenApiEnumFlagsExtension extension = new();
        using TextWriter sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter);

        // Act
        extension.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();

        // Assert
        Assert.Contains("\"isFlags\": false", result);
        Assert.DoesNotContain("\"style\"", result);
        Assert.False(extension.IsFlags);
    }

    [Fact]
    public void WritesAllDefaultValues()
    {
        // Arrange
        OpenApiEnumFlagsExtension extension = new()
        {
            IsFlags = true
        };
        using TextWriter sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter);

        // Act
        extension.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();

        // Assert
        Assert.Contains("\"isFlags\": true", result);
        Assert.True(extension.IsFlags);
    }

    [Fact]
    public void WritesAllValues()
    {
        // Arrange
        OpenApiEnumFlagsExtension extension = new()
        {
            IsFlags = true
        };
        using TextWriter sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter);

        // Act
        extension.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();

        // Assert
        Assert.True(extension.IsFlags);
        Assert.Contains("\"isFlags\": true", result);
    }
}

