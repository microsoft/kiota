using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public sealed class CodeIndexerWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClass parentClass;
    private readonly CodeIndexer indexer;
    public CodeIndexerWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        indexer = new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = "SomeRequestBuilder"
            },
            IndexParameter = new()
            {
                Name = "position",
                Type = new CodeType
                {
                    Name = "string",
                },
                SerializationName = "id",
                Documentation = new()
                {
                    DescriptionTemplate = "some description"
                }
            }
        };
        parentClass.AddIndexer(indexer);
        parentClass.AddProperty(new()
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "string"
            }
        }, new()
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "string"
            }
        });
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesIndexer()
    {
        writer.Write(indexer);
        var result = tw.ToString();
        Assert.Contains("RequestAdapter", result);
        Assert.Contains("PathParameters", result);
        Assert.Contains("id\", position", result);
        Assert.Contains("some description", result);
        Assert.Contains("public SomeRequestBuilder this[string position]", result);
        Assert.Contains("return new SomeRequestBuilder(urlTplParams, RequestAdapter);", result);
        Assert.Contains("<returns>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
}
