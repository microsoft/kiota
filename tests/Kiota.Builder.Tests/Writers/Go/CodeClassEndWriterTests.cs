using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;
public class CodeClassEndWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeBlockEndWriter codeElementWriter;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    public CodeClassEndWriterTests()
    {
        codeElementWriter = new CodeBlockEndWriter();
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void ClosesNestedClasses()
    {
        var child = parentClass.AddInnerClass(new CodeClass
        {
            Name = "child"
        }).First();
        codeElementWriter.WriteCodeElement(child.EndBlock, writer);
        var result = tw.ToString();
        Assert.Equal(1, result.Count(static x => x == '}'));
    }
    [Fact]
    public void ClosesNonNestedClasses()
    {
        codeElementWriter.WriteCodeElement(parentClass.EndBlock, writer);
        var result = tw.ToString();
        Assert.Equal(1, result.Count(static x => x == '}'));
    }
    [Fact]
    public void WritesMessageOverrideOnPrimary()
    {
        // Given
        parentClass.IsErrorDefinition = true;
        var prop1 = parentClass.AddProperty(new CodeProperty
        {
            Name = "prop1",
            Kind = CodePropertyKind.Custom,
            IsPrimaryErrorMessage = true,
            Type = new CodeType
            {
                Name = "string",
            },
        }).First();
        parentClass.AddMethod(new CodeMethod
        {
            Name = "GetProp1",
            Kind = CodeMethodKind.Getter,
            ReturnType = prop1.Type,
            Access = AccessModifier.Public,
            AccessedProperty = prop1,
            IsAsync = false,
            IsStatic = false,
        });
        var parentInterface = root.AddInterface(new CodeInterface
        {
            Name = "parentInterface",
            OriginalClass = parentClass,
        }).First();
        parentInterface.AddMethod(new CodeMethod
        {
            Name = "GetProp1",
            Kind = CodeMethodKind.Getter,
            ReturnType = prop1.Type,
            Access = AccessModifier.Public,
            AccessedProperty = prop1,
            IsAsync = false,
            IsStatic = false,
        });

        // When
        codeElementWriter.WriteCodeElement(parentInterface.EndBlock, writer);
        var result = tw.ToString();

        // Then
        Assert.Contains("Error() string {", result);
        Assert.Contains("return *(e.GetProp1()", result);
    }
}
