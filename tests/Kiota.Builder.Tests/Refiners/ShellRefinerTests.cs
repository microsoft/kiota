using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;

public class ShellRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

    [Fact]
    public void AddsUsingsForCommandTypesUsedInCommandBuilder() {
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var commandBuilder = requestBuilder.AddMethod(new CodeMethod {
            Name = "GetCommand",
            MethodKind = CodeMethodKind.CommandBuilder,
            ReturnType = new CodeType {
                Name = "Command",
                IsExternal = true
            }
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Shell }, root);
        
        var declaration = requestBuilder.StartBlock as CodeClass.Declaration;

        Assert.Contains("System.CommandLine", declaration.Usings.Select(x => x.Declaration?.Name));
    }

    [Fact]
    public void CreatesCommandBuilders() {
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        // Add nav props
        requestBuilder.AddProperty(new CodeProperty {
            Name = "User",
            PropertyKind = CodePropertyKind.RequestBuilder
        });

        // Add indexer
        requestBuilder.SetIndexer(new CodeIndexer {
            Name = "Users",
            ReturnType = new CodeType {
                Name = "Address"
            }
        });

        // Add request executor
        requestBuilder.AddMethod(new CodeMethod {
            Name = "GetExecutor",
            ReturnType = new CodeType {
                Name = "User"
            },
            MethodKind = CodeMethodKind.RequestExecutor,
            HttpMethod = HttpMethod.Get
        });

        // Add client constructor
        requestBuilder.AddMethod(new CodeMethod {
            Name = "constructor",
            MethodKind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType {
                Name = "void"
            },
            DeserializerModules = new() {"com.microsoft.kiota.serialization.Deserializer"},
            SerializerModules = new() {"com.microsoft.kiota.serialization.Serializer"}
        });

        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Shell }, root);

        var methods = root.GetChildElements().OfType<CodeClass>().SelectMany(c => c.GetChildElements().OfType<CodeMethod>());
        var methodNames = methods.Select(m => m.Name);

        Assert.Contains("BuildCommand", methodNames);
        Assert.Contains("BuildUserCommand", methodNames);
        Assert.Contains("BuildListCommand", methodNames);
        Assert.Contains("BuildRootCommand", methodNames);
    }
}
