using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public class CodeNameSpaceWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;

    public CodeNameSpaceWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ExportsInterfacesAndFunctions()
    {
        var root = CodeNamespace.InitRootNamespace();
        var modelInterface = new CodeInterface
        {
            Name = "ModelInterface",
            Kind = CodeInterfaceKind.Model
        };
        var modelEnum = new CodeEnum
        {
            Name = "TestEnum", // The tests should verify if the printed file names start with lower case.
        };
        root.AddEnum(modelEnum);
        root.AddInterface(modelInterface);
        writer.Write(root);
        var result = tw.ToString();
        Console.WriteLine(result);
        Assert.Contains($"export * from './testEnum'{Environment.NewLine}export * from './modelInterface'{Environment.NewLine}", result);
    }

}
