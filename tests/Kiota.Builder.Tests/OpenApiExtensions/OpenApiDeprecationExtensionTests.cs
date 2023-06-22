using System;
using System.IO;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Writers;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public class OpenApiDeprecationExtensionTests
{
    [Fact]
    public void Parses()
    {
        var oaiValue = new OpenApiObject
        {
            { "date", new OpenApiDateTime(new DateTimeOffset(2023,05,04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)))},
            { "removalDate", new OpenApiDateTime(new DateTimeOffset(2023,05,04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)))},
            { "version", new OpenApiString("v1.0")},
            { "description", new OpenApiString("removing")}
        };
        var value = OpenApiDeprecationExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("v1.0", value.Version);
        Assert.Equal("removing", value.Description);
        Assert.Equal(new DateTimeOffset(2023, 05, 04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)), value.Date);
        Assert.Equal(new DateTimeOffset(2023, 05, 04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)), value.RemovalDate);
    }
    [Fact]
    public void Serializes()
    {
        var value = new OpenApiDeprecationExtension
        {
            Date = new DateTimeOffset(2023, 05, 04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)),
            RemovalDate = new DateTimeOffset(2023, 05, 04, 16, 0, 0, 0, 0, new TimeSpan(4, 0, 0)),
            Version = "v1.0",
            Description = "removing"
        };
        using TextWriter sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter);


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\n  \"removalDate\": \"2023-05-04T16:00:00.0000000+04:00\",\n  \"date\": \"2023-05-04T16:00:00.0000000+04:00\",\n  \"version\": \"v1.0\",\n  \"description\": \"removing\"\n}", result);
    }
}
