using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public class CodePropertyWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "Name";
    private readonly CodeClass parentClass;
    private readonly CodePropertyWriter propertyWriter;
    private readonly LanguageWriter languageWriter;
    private readonly StringWriter stringWriter;
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    private readonly ILanguageRefiner phpRefiner = new PhpRefiner(new GenerationConfiguration { Language = GenerationLanguage.PHP });

    public CodePropertyWriterTests()
    {
        stringWriter = new StringWriter();
        languageWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
        languageWriter.SetTextWriter(stringWriter);
        parentClass = new CodeClass
        {
            Name = "ParentClass",
            Documentation = new()
            {
                DescriptionTemplate = "This is an amazing class",
            },
            Kind = CodeClassKind.Model
        };
        root.AddClass(parentClass);
        propertyWriter = new CodePropertyWriter(new PhpConventionService());
    }
    [Fact]
    public void WritePropertyDocs()
    {
        var property = new CodeProperty
        {
            Name = "Email",
            Access = AccessModifier.Private,
            Type = new CodeType
            {
                Name = "emailAddress"
            }
        };
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);

        var result = stringWriter.ToString();
        Assert.Contains("@var EmailAddress|null $email", result);
        Assert.Contains("private ?EmailAddress $email = null;", result);
    }

    [Fact]
    public void WritePropertyRequestBuilder()
    {
        var property = new CodeProperty
        {
            Name = "message",
            Access = AccessModifier.Public,
            Documentation = new()
            {
                DescriptionTemplate = "I can get your messages.",
            },
            Type = new CodeType
            {
                Name = "MessageRequestBuilder"
            },
            Kind = CodePropertyKind.RequestBuilder
        };
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);

        var result = stringWriter.ToString();
        Assert.Contains("public function message(): MessageRequestBuilder", result);
        Assert.Contains("return new MessageRequestBuilder($this->pathParameters, $this->requestAdapter);", result);
    }

    [Fact]
    public async Task WriteCollectionKindPropertyAsync()
    {
        var property = new CodeProperty
        {
            Documentation = new()
            {
                DescriptionTemplate = "Additional data dictionary",
            },
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Access = AccessModifier.Private,
            Type = new CodeType { Name = "array", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }
        };
        parentClass.Kind = CodeClassKind.Model;
        parentClass.AddProperty(property);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
        propertyWriter.WriteCodeElement(property, languageWriter);
        var result = stringWriter.ToString();
        Assert.Contains("private ?array $additionalData = null;", result);
        Assert.Contains("@var array<string, mixed>|null", result);
    }

    [Fact]
    public void WriteCollectionNonAdditionalData()
    {
        var property = new CodeProperty
        {
            Name = "recipients",
            Kind = CodePropertyKind.Custom,
            Access = AccessModifier.Private,
            Type = new CodeType
            {
                Name = "recipient",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array
            }
        };
        parentClass.AddProperty(property);

        propertyWriter.WriteCodeElement(property, languageWriter);
        var result = stringWriter.ToString();
        Assert.Contains("@var array<Recipient>|null", result);
    }

    [Fact]
    public void WriteRequestAdapter()
    {
        var adapter = new CodeProperty
        {
            Name = "adapter",
            Type = new CodeType { Name = "requestAdapter", IsNullable = false },
            Access = AccessModifier.Private,
            Kind = CodePropertyKind.RequestAdapter
        };
        parentClass.AddProperty(adapter);
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathSegment",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType { Name = "string", IsNullable = false },
        });
        propertyWriter.WriteCodeElement(adapter, languageWriter);
        var result = stringWriter.ToString();

        Assert.Contains("private RequestAdapter $adapter;", result);
    }

    [Fact]
    public void WritePrimitiveFloatProperty()
    {
        CodeProperty property = new CodeProperty
        {
            Name = "property",
            Type = new CodeType
            {
                Name = "double"
            },
            Access = AccessModifier.Protected
        };
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);
        var result = stringWriter.ToString();
        Assert.Contains("protected ?float $property = null;", result);
    }

    public static IEnumerable<object[]> StringProperties => new List<object[]>
    {
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "decimal" } } },
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "byte" } } }
    };

    [Theory]
    [MemberData(nameof(StringProperties))]
    public void WritePrimitiveStringProperty(CodeProperty property)
    {
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);
        Assert.Contains("public ?string $property = null;", stringWriter.ToString());
    }

    public static IEnumerable<object[]> IntProperties => new List<object[]>
    {
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "integer" }, Access = AccessModifier.Protected} },
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "int32" }, Access = AccessModifier.Protected} },
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "sbyte" }, Access = AccessModifier.Protected} },
        new object[] { new CodeProperty { Name = "property", Type = new CodeType { Name = "int64" }, Access = AccessModifier.Protected} }
    };
    [Theory]
    [MemberData(nameof(IntProperties))]
    public void WritePrimitiveIntProperty(CodeProperty property)
    {
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);
        Assert.Contains("protected ?int $property = null;", stringWriter.ToString());
    }

    [Fact]
    public void WriteQueryParameter()
    {
        var queryParameter = new CodeProperty
        {
            Name = "select",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24select",
            Access = AccessModifier.Private,
            Type = new CodeType
            {
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                Name = "string"
            }
        };
        parentClass.AddProperty(queryParameter);
        propertyWriter.WriteCodeElement(queryParameter, languageWriter);
        var result = stringWriter.ToString();

        Assert.Contains("@QueryParameter(\"%24select\")", result);
        Assert.Contains("@var array<string>|null $select", result);
        Assert.Contains("private ?array $select", result);
    }

    [Fact]
    public async Task WriteRequestOptionAsync()
    {
        var options = new CodeProperty
        {
            Name = "options",
            Kind = CodePropertyKind.Options,
            Access = AccessModifier.Public,
            Type = new CodeType
            {
                Name = "IList<IRequestOption>",
                IsExternal = true
            }
        };
        parentClass.AddProperty(options);
        await phpRefiner.RefineAsync(root, new CancellationToken());
        propertyWriter.WriteCodeElement(options, languageWriter);
        var result = stringWriter.ToString();

        Assert.Contains("@var array<RequestOption>|null $options", result);
        Assert.Contains("public ?array $options = null;", result);
    }

    [Fact]
    public void WritePropertyWithDescription()
    {
        CodeProperty property = new CodeProperty
        {
            Name = "name",
            Documentation = new()
            {
                DescriptionTemplate = "The name pattern that branches must match in order to deploy to the environment.Wildcard characters will not match `/`. For example, to match branches that begin with `release/` and contain an additional single slash, use `release/*/*`.For more information about pattern matching syntax, see the [Ruby File.fnmatch documentation](https://ruby-doc.org/core-2.5.1/File.html#method-c-fnmatch).",
            },
            Type = new CodeType
            {
                Name = "string"
            },
            Access = AccessModifier.Private
        };
        parentClass.AddProperty(property);
        propertyWriter.WriteCodeElement(property, languageWriter);
        var result = stringWriter.ToString();
        Assert.DoesNotContain("/*/*", result);
    }
}
