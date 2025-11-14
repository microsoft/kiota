using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    public CodeClassDeclarationWriterTests()
    {
        codeElementWriter = new CodeClassDeclarationWriter(new PhpConventionService());
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        root.Name = "Microsoft\\Graph";
        parentClass = new()
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
    public void WritesSimpleDeclaration()
    {
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("class ParentClass", result);
    }
    [Fact]
    public void WritesImplementation()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddImplements(new CodeType
        {
            Name = "\\Stringable"
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("implements \\Stringable", result);
    }
    [Fact]
    public void WritesInheritance()
    {
        var declaration = parentClass.StartBlock;
        declaration.Inherits = new()
        {
            Name = "someInterface"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("extends", result);
    }
    [Fact]
    public void WritesImports()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddUsings(new()
        {
            Name = "Promise",
            Declaration = new()
            {
                Name = "Http\\Promise\\",
                IsExternal = true,
            }
        },
        new()
        {
            Name = "Microsoft\\Graph\\Models",
            Declaration = new()
            {
                Name = "Message",
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("use Microsoft\\Graph\\Models\\Message", result);
        Assert.Contains("use Http\\Promise\\Promise", result);
    }
    [Fact]
    public void RemovesImportWithClassName()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddUsings(new CodeUsing
        {
            Name = "Microsoft\\Graph\\Models",
            Declaration = new()
            {
                Name = "ParentClass",
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("Microsoft\\Graph\\Models\\ParentClass", result);
    }

    [Fact]
    public async Task ImportRequiredClassesWhenContainsRequestExecutorAsync()
    {
        var declaration = parentClass;
        declaration?.AddMethod(new CodeMethod
        {
            Name = "get",
            Access = AccessModifier.Public,
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = HttpMethod.Get,
            ReturnType = new CodeType
            {
                Name = "Promise",
                Parent = declaration
            }
        });
        var dec = declaration?.StartBlock;
        var namespaces = declaration?.Parent as CodeNamespace;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.PHP }, namespaces);
        codeElementWriter.WriteCodeElement(dec, writer);
        var result = tw.ToString();

        Assert.Contains(@"use Http\Promise\Promise;", result);
        Assert.Contains("use Exception;", result);
    }

    [Fact]
    public void ExtendABaseClass()
    {
        var currentClass = parentClass.StartBlock;
        if (currentClass != null)
        {
            currentClass.Inherits = new CodeType
            {
                TypeDefinition = new CodeClass { Name = "Model", Kind = CodeClassKind.Custom }
            };
        }

        codeElementWriter.WriteCodeElement(currentClass, writer);
        var result = tw.ToString();
        Assert.Contains("extends", result);
    }

    [Fact]
    public async Task AddsImportsToRequestConfigClassesAsync()
    {
        var queryParamClass = new CodeClass { Name = "TestRequestQueryParameter", Kind = CodeClassKind.QueryParameters };
        var modelsNamespace = root.AddNamespace("Models");
        var eventStatus = new CodeEnum { Name = "EventStatus" };
        modelsNamespace.AddEnum(eventStatus);
        queryParamClass.AddProperty(new[]
        {
            new CodeProperty
            {
                Name = "startTime",
                Kind = CodePropertyKind.QueryParameter,
                Documentation = new()
                {
                    DescriptionTemplate = "Filter by start time",
                },
                Type = new CodeType
                {
                    Name = "datetimeoffset"
                },
            },
            new CodeProperty
            {
                Name = "endTime",
                Kind = CodePropertyKind.QueryParameter,
                Documentation = new()
                {
                    DescriptionTemplate = "Filter by end time",
                },
                Type = new CodeType
                {
                    Name = "datetimeoffset"
                },
            },
            new CodeProperty
            {
                Name = "startDate",
                Kind = CodePropertyKind.QueryParameter,
                Documentation = new()
                {
                    DescriptionTemplate = "Filter by start date",
                },
                Type = new CodeType
                {
                    Name = "dateonly"
                },
            },
            new CodeProperty
            {
                Name = "status",
                Kind = CodePropertyKind.QueryParameter,
                Documentation = new()
                {
                    DescriptionTemplate = "Filter by status",
                },
                Type = new CodeType
                {
                    Name = "EventStatus",
                    TypeDefinition = eventStatus
                },
            }
        });
        root.AddClass(queryParamClass);
        parentClass.Kind = CodeClassKind.RequestConfiguration;
        parentClass.AddProperty(new[] {
            new CodeProperty
            {
                Name = "queryParameters",
                Kind = CodePropertyKind.QueryParameters,
                Documentation = new() { DescriptionTemplate = "Request query parameters", },
                Type = new CodeType { Name = queryParamClass.Name, TypeDefinition = queryParamClass },
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.PHP, UsesBackingStore = true }, root);
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();

        Assert.Contains("use DateTime;", result);
        Assert.Contains("use Microsoft\\Kiota\\Abstractions\\Types\\Date;", result);
        Assert.Contains("use Microsoft\\Graph\\Models\\EventStatus;", result);

    }
}
