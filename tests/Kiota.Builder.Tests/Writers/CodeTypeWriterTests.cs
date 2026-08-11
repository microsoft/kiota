using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers;

public sealed class CodeTypeWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeType currentType;
    private const string TypeName = "SomeType";
    public CodeTypeWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        currentType = new()
        {
            Name = TypeName
        };
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ParentClass"
        }).First();
        currentType.Parent = parentClass;
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesCodeType()
    {
        writer.Write(currentType);
        var result = tw.ToString();
        Assert.Contains(TypeName, result);
    }
}
