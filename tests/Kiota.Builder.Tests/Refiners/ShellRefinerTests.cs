using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class ShellRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

    [Fact]
    public async Task AddsUsingsForCommandTypesUsedInCommandBuilder()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var commandBuilder = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "GetCommand",
            Kind = CodeMethodKind.CommandBuilder,
            ReturnType = new CodeType
            {
                Name = "Command",
                IsExternal = true
            }
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Shell }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("System.CommandLine", declaration.Usings.Select(x => x.Declaration?.Name));
    }

    [Fact]
    public async Task CreatesCommandBuilders()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        // Add nav props
        requestBuilder.AddProperty(new CodeProperty
        {
            Name = "User",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType
            {
                Name = "UserRequestBuilder",
                IsExternal = true
            }
        });

        // Add indexer
        requestBuilder.Indexer = new CodeIndexer
        {
            Name = "Users-idx",
            ReturnType = new CodeType
            {
                Name = "Address"
            },
            IndexType = new CodeType
            {
                Name = "string"
            },
        };

        // Add request executor
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "GetExecutor",
            ReturnType = new CodeType
            {
                Name = "User"
            },
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = HttpMethod.Get
        });

        // Add request executor
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "PostExecutor",
            ReturnType = new CodeType
            {
                Name = "User"
            },
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = HttpMethod.Post
        });

        // Add request executor
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "PutTest",
            ReturnType = new CodeType
            {
                Name = "User"
            },
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = HttpMethod.Put
        });

        // Add client constructor
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType
            {
                Name = "void"
            },
            DeserializerModules = new() { "com.microsoft.kiota.serialization.Deserializer" },
            SerializerModules = new() { "com.microsoft.kiota.serialization.Serializer" }
        });

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Shell }, root);

        var methods = root.GetChildElements().OfType<CodeClass>().SelectMany(c => c.Methods);
        var methodNames = methods.Select(m => m.Name);

        Assert.Contains("BuildCommand", methodNames);
        Assert.Contains("BuildUserNavCommand", methodNames);
        Assert.Contains("BuildCreateCommand", methodNames);
        Assert.Contains("BuildListCommand", methodNames);
        Assert.Contains("BuildPutCommand", methodNames);
        Assert.Contains("BuildRootCommand", methodNames);

        Assert.Contains("Put", methods.Single(m => m.OriginalMethod != null && m.OriginalMethod.Name == "PutTest").SimpleName);
        Assert.Contains("Create", methods.Single(m => m.OriginalMethod != null && m.OriginalMethod.Name == "PostExecutor").SimpleName);
        Assert.Contains("List", methods.Single(m => m.OriginalMethod != null && m.OriginalMethod.Name == "GetExecutor").SimpleName);
        Assert.Contains("UsersIdx", methods.Single(m => m.OriginalIndexer != null && m.OriginalIndexer.Name == "Users-idx").SimpleName);
        Assert.Contains("User", methods.Single(m => m.AccessedProperty != null && m.AccessedProperty.Name == "User").SimpleName);
        Assert.Contains(string.Empty, methods.Single(m => m.OriginalMethod != null && m.OriginalMethod.Kind == CodeMethodKind.ClientConstructor).SimpleName);
        Assert.Contains(string.Empty, methods.Single(m => m.Kind == CodeMethodKind.ClientConstructor).SimpleName);
    }

    [Fact]
    public async Task RemovesRequestAdaptersFromCodeDom()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed

        requestBuilder.AddProperty(new CodeProperty
        {
            Name = "adapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true
            }
        });

        // Add client constructor
        var clientCtor = new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType
            {
                Name = "void"
            },
            DeserializerModules = new() { "com.microsoft.kiota.serialization.Deserializer" },
            SerializerModules = new() { "com.microsoft.kiota.serialization.Serializer" }
        };
        clientCtor.AddParameter(new CodeParameter
        {
            Name = "adapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "void"
            }
        });
        requestBuilder.AddMethod(clientCtor);

        var classes = root.GetChildElements().OfType<CodeClass>();
        var methods = classes.SelectMany(c => c.Methods);
        var properties = classes.SelectMany(c => c.Properties);
        var methodNames = methods.Select(x => x.Name);
        var propertyNames = properties.Select(m => m.Name);
        var methodParamNames = methods.SelectMany(m => m.Parameters).Select(x => x.Name);
        Assert.Contains("adapter", propertyNames);
        Assert.Contains("adapter", methodParamNames);

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Shell }, root);

        Assert.DoesNotContain("adapter", propertyNames);
        Assert.DoesNotContain("adapter", methodParamNames);
        Assert.Contains("BuildRootCommand", methodNames);
    }
}
