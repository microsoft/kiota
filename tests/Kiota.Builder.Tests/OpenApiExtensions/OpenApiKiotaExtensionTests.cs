using System.IO;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Writers;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public class OpenApiKiotaExtensionTests
{
    [Fact]
    public void Serializes()
    {
        var value = new OpenApiKiotaExtension
        {
            LanguagesInformation = new() {
                {
                    "CSharp",
                    new LanguageInformation {
                        Dependencies = new() {
                            new LanguageDependency {
                                Name = "Microsoft.Graph.Core",
                                Version = "1.0.0",
                            }
                        },
                        DependencyInstallCommand = "dotnet add package",
                        MaturityLevel = LanguageMaturityLevel.Preview,
                        ClientClassName = "GraphServiceClient",
                        ClientNamespaceName = "Microsoft.Graph",
                        StructuredMimeTypes = new() {
                            "application/json",
                            "application/xml",
                        }
                    }
                }
            },
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\"languagesInformation\":{\"CSharp\":{\"maturityLevel\":\"Preview\",\"dependencyInstallCommand\":\"dotnet add package\",\"dependencies\":[{\"name\":\"Microsoft.Graph.Core\",\"version\":\"1.0.0\"}],\"clientClassName\":\"GraphServiceClient\",\"clientNamespaceName\":\"Microsoft.Graph\",\"structuredMimeTypes\":[\"application/json\",\"application/xml\"]}}}", result);
    }
    [Fact]
    public void Parses()
    {
        var oaiValue = new OpenApiObject
        {
            { "languagesInformation", new OpenApiObject {
                {"CSharp", new OpenApiObject {
                        {"dependencies", new OpenApiArray {
                            new OpenApiObject {
                                {"name", new OpenApiString("Microsoft.Graph.Core")},
                                {"version", new OpenApiString("1.0.0") },
                            }
                        }},
                        {"dependencyInstallCommand", new OpenApiString("dotnet add package") },
                        {"maturityLevel", new OpenApiString("Preview")},
                        {"clientClassName", new OpenApiString("GraphServiceClient")},
                        {"clientNamespaceName", new OpenApiString("Microsoft.Graph")},
                        {"structuredMimeTypes", new OpenApiArray {
                            new OpenApiString("application/json"),
                            new OpenApiString("application/xml")}
                        },
                    }
                }
            }}
        };
        var value = OpenApiKiotaExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.True(value.LanguagesInformation.TryGetValue("CSharp", out var CSEntry));
        Assert.Equal("dotnet add package", CSEntry.DependencyInstallCommand);
        Assert.Equal(LanguageMaturityLevel.Experimental, CSEntry.MaturityLevel); //expected as we're not parsing the value from the description
        Assert.Equal("GraphServiceClient", CSEntry.ClientClassName);
        Assert.Equal("Microsoft.Graph", CSEntry.ClientNamespaceName);
        Assert.Single(CSEntry.Dependencies);
        Assert.Equal("Microsoft.Graph.Core", CSEntry.Dependencies[0].Name);
        Assert.Equal(2, CSEntry.StructuredMimeTypes.Count);
        Assert.Contains("application/json", CSEntry.StructuredMimeTypes);
        Assert.Contains("application/xml", CSEntry.StructuredMimeTypes);
        Assert.Equal("1.0.0", CSEntry.Dependencies[0].Version);
    }
}
