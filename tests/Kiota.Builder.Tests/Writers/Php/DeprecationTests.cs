using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public class DeprecationTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("enum")]
    [InlineData("property")]
    [InlineData("navigation")]
    [InlineData("method")]
    public void WritesDeprecationWithoutDocumentation(string kind)
    {
        var result = WriteElement(kind, new DeprecationInformation("Use the replacement", Version: "2.0"));
        Assert.Contains(" * @deprecated 2.0 Use the replacement", result);
        Assert.Contains($" * @deprecated{Environment.NewLine}", WriteElement(kind, new DeprecationInformation(null)));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("enum")]
    [InlineData("property")]
    [InlineData("navigation")]
    [InlineData("method")]
    public void DoesNotDeprecateCurrentElements(string kind)
    {
        Assert.DoesNotContain("@deprecated", WriteElement(kind, null));
        Assert.DoesNotContain("@deprecated", WriteElement(kind, new DeprecationInformation("Current", IsDeprecated: false)));
    }

    [Fact]
    public void WritesDeprecationDatesAndNeutralizesCommentDelimiters()
    {
        var result = WriteElement("class", new DeprecationInformation(
            "Use */ \"replacement\" /* '$value'\n\r\t\\next", new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 3, 4, 0, 0, 0, TimeSpan.Zero), "2.0*/"));
        Assert.Contains("@deprecated 2.0* /", result);
        Assert.Contains("Use * /", result);
        Assert.Contains("//*", result);
        Assert.Contains("on 2024-01-02", result);
        Assert.Contains("will be removed 2025-03-04", result);
        Assert.DoesNotContain("Use */", result);
        Assert.DoesNotContain("2.0*/", result);
    }

    private static string WriteElement(string kind, DeprecationInformation deprecation)
    {
        var root = CodeNamespace.InitRootNamespace();
        root.Name = "Api";
        var parentClass = root.AddClass(new CodeClass { Name = "legacy", Deprecation = kind == "class" ? deprecation : null }).First();
        using var text = new StringWriter();
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, "./", "client");
        writer.SetTextWriter(text);
        switch (kind)
        {
            case "class":
                writer.Write(parentClass.StartBlock);
                break;
            case "enum":
                var codeEnum = root.AddEnum(new CodeEnum { Name = "status", Deprecation = deprecation }).First();
                codeEnum.AddOption(new CodeEnumOption { Name = "old" });
                writer.Write(codeEnum);
                break;
            case "method":
                writer.Write(parentClass.AddMethod(new CodeMethod { Name = "legacyMethod", Deprecation = deprecation, ReturnType = new CodeType { Name = "void" } }).First());
                break;
            default:
                writer.Write(parentClass.AddProperty(new CodeProperty
                {
                    Name = "legacyProperty",
                    Deprecation = deprecation,
                    Kind = kind == "navigation" ? CodePropertyKind.RequestBuilder : CodePropertyKind.Custom,
                    Type = new CodeType { Name = "string" }
                }).First());
                break;
        }
        return text.ToString();
    }
}
