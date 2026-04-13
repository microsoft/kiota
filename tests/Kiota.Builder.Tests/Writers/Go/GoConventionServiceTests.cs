using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class GoConventionServiceTests
{
    private readonly GoConventionService instance = new();
    [Fact]
    public void ThrowsOnInvalidOverloads()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
    }
    [Fact]
    public void SanitizesLineBreaksInDocumentationComments()
    {
        var codeClass = new CodeClass
        {
            Name = "testClass",
            Documentation = new()
            {
                DescriptionTemplate = "line1\r\nline2\tline3",
            },
        };
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, "./", "name");
        using var textWriter = new StringWriter();
        writer.SetTextWriter(textWriter);

        instance.WriteShortDescription(codeClass, writer);
        var result = textWriter.ToString();

        Assert.Contains("// line1line2 line3", result);
        Assert.DoesNotContain($"{Environment.NewLine}line2", result);
    }
}
