using System.IO;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Writers;
using Moq;
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
                    }
                }
            },
        };
        using TextWriter sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter);


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\n  \"languagesInformation\": {\n    \"CSharp\": {\n      \"maturityLevel\": \"Preview\",\n      \"dependencyInstallCommand\": \"dotnet add package\",\n      \"dependencies\": [\n        {\n          \"name\": \"Microsoft.Graph.Core\",\n          \"version\": \"1.0.0\"\n        }\n      ]\n    }\n  }\n}", result);
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
                    }
                }
            }}
        };
        var value = OpenApiKiotaExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.True(value.LanguagesInformation.TryGetValue("CSharp", out var CSEntry));
        Assert.Equal("dotnet add package", CSEntry.DependencyInstallCommand);
        Assert.Equal(LanguageMaturityLevel.Preview, CSEntry.MaturityLevel);
        Assert.Single(CSEntry.Dependencies);
        Assert.Equal("Microsoft.Graph.Core", CSEntry.Dependencies[0].Name);
        Assert.Equal("1.0.0", CSEntry.Dependencies[0].Version);
    }
}
