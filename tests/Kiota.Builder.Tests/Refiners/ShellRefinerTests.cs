﻿using System.Linq;
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
        requestBuilder.SetIndexer(new CodeIndexer
        {
            Name = "Users",
            ReturnType = new CodeType
            {
                Name = "Address"
            },
            IndexType = new CodeType
            {
                Name = "string"
            },
        });

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

        var methods = root.GetChildElements().OfType<CodeClass>().SelectMany(c => c.GetChildElements().OfType<CodeMethod>());
        var methodNames = methods.Select(m => m.Name);

        Assert.Contains("BuildCommand", methodNames);
        Assert.Contains("BuildUserCommand", methodNames);
        Assert.Contains("BuildListCommand", methodNames);
        Assert.Contains("BuildRootCommand", methodNames);
    }
}
