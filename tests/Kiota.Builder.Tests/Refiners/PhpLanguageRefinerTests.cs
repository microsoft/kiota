using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class PhpLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

    [Fact]
    public async Task ReplacesRequestBuilderPropertiesByMethods()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "userRequestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();

        var requestBuilder = model.AddProperty(new CodeProperty
        {
            Name = "breaks",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        Assert.Equal("breaks", requestBuilder.Name);
        Assert.Equal("userRequestBuilder", model.Name);
    }

    [Fact]
    public async Task PrefixReservedWordPropertyNamesWith()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "userRequestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();

        var property = model.AddProperty(new CodeProperty
        {
            Name = "continue",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        Assert.Equal("EscapedContinue", property.Name);
    }

    [Fact]
    public async Task ReplacesBinaryWithNativeType()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "binary"
            }
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        Assert.Equal("StreamInterface", method.ReturnType.Name);
    }

    [Fact]
    public async Task AddsDefaultImports()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        root.AddClass(new CodeClass
        {
            Name = "rb",
            Kind = CodeClassKind.RequestBuilder,
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
    }

    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "SomeModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        model.AddProperty(
            new CodeProperty
            {
                Type = new CodeType
                {
                    Name = "string"
                },
                Name = "code",
            },
            new CodeProperty
            {
                Type = new CodeType
                {
                    Name = "integer"
                },
                Name = "message",
            }
        );
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
        Assert.Contains("escapedMessage", model.Properties.Select(x => x.Name));
        Assert.Contains("escapedCode", model.Properties.Select(x => x.Name));
    }

    [Fact]
    public async Task ChangesBackingStoreParameterTypeInApiClientConstructor()
    {
        var apiClientClass = new CodeClass { Name = "ApiClient", Kind = CodeClassKind.Custom };
        var constructor = new CodeMethod
        {
            Name = "ApiClientConstructor",
            Kind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType { Name = "string" },
        };
        var backingStoreParameter = new CodeParameter
        {
            Name = "BackingStore",
            Kind = CodeParameterKind.BackingStore,
            Type = new CodeType
            {
                Name = "IBackingStoreFactory",
                IsExternal = true
            }
        };
        constructor.AddParameter(backingStoreParameter);
        constructor.DeserializerModules = new() { "Microsoft\\Kiota\\Serialization\\Deserializer" };
        constructor.SerializerModules = new() { "Microsoft\\Kiota\\Serialization\\Serializer" };
        apiClientClass.AddMethod(constructor);

        root.AddClass(apiClientClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP, UsesBackingStore = true }, root);
        Assert.Equal("BackingStoreFactory", backingStoreParameter.Type.Name);
        Assert.Equal("null", backingStoreParameter.DefaultValue);
    }

    [Fact]
    public async Task ImportsClassForDiscriminatorReturns()
    {
        var modelClass = new CodeClass
        {
            Name = "Entity",
            Parent = root,
            Kind = CodeClassKind.Model,
            DiscriminatorInformation = new DiscriminatorInformation
            {
                Name = "createFromDiscriminatorValue",
                DiscriminatorPropertyName = "@odata.type",
            }
        };
        var parentClass = new CodeClass { Name = "ParentClass", Kind = CodeClassKind.Model, Parent = root };
        var subNamespace = root.AddNamespace("Security");
        subNamespace.Parent = root;
        root.AddClass(modelClass);

        var securityClass = new CodeClass { Name = "Security", Parent = subNamespace, Kind = CodeClassKind.Model };
        var codeMethod = new CodeMethod
        {
            Name = "createFromDiscriminatorValue",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType { TypeDefinition = modelClass, Name = "Entity" }
        };
        codeMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Type = new CodeType { Name = "ParseNode", IsExternal = true, },
            Kind = CodeParameterKind.ParseNode
        });
        modelClass.DiscriminatorInformation.AddDiscriminatorMapping("#models.security",
            new CodeType { Name = "Security", TypeDefinition = securityClass, });
        var tagClass = new CodeClass { Name = "Tag", Kind = CodeClassKind.Model, Parent = modelClass.Parent };
        root.AddClass(tagClass);

        modelClass.DiscriminatorInformation.AddDiscriminatorMapping("#models.security.Tag",
            new CodeType { Name = "Tag", TypeDefinition = tagClass, });
        modelClass.DiscriminatorInformation.AddDiscriminatorMapping("#models.ParentClass",
            new CodeType { Name = "ParentClass", TypeDefinition = parentClass, });

        modelClass.DiscriminatorInformation.AddDiscriminatorMapping("#models.entity",
            new CodeType { Name = "Entity", TypeDefinition = modelClass, });
        modelClass.AddMethod(codeMethod);
        securityClass.StartBlock.Inherits = new CodeType
        {
            Name = "Entity",
            IsExternal = false,
            TypeDefinition = modelClass
        };
        Assert.Empty(modelClass.Usings);
        subNamespace.AddClass(securityClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        Assert.Equal(2, modelClass.Usings.Count());
    }
}
