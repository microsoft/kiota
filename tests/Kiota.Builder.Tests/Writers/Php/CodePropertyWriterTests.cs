using System.Collections.Generic;
using System.IO;
using Kiota.Builder.Refiners;
using Xunit;

namespace Kiota.Builder.Writers.Php.Tests
{
    public class CodePropertyWriterTests
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "Name";
        private readonly CodeClass parentClass;
        private readonly CodePropertyWriter propertyWriter;
        private readonly LanguageWriter languageWriter;
        private readonly StringWriter stringWriter;
        private readonly ILanguageRefiner _refiner;
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

        public CodePropertyWriterTests()
        {
            stringWriter = new StringWriter();
            languageWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            languageWriter.SetTextWriter(stringWriter);
            parentClass = new CodeClass()
            {
                Name = "ParentClass", Description = "This is an amazing class", Kind = CodeClassKind.Model
            };
            root.AddClass(parentClass);
            _refiner = new PhpRefiner(new() {Language = GenerationLanguage.PHP});
            propertyWriter = new CodePropertyWriter(new PhpConventionService());
        }
        [Fact]
        public void WritePropertyDocs()
        {
            var property = new CodeProperty()
            {
                Name = "email",
                Access = AccessModifier.Private,
                Type = new CodeType()
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
            var property = new CodeProperty()
            {
                Name = "message",
                Access = AccessModifier.Public,
                Description = "I can get your messages.",
                Type = new CodeType()
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
        public void WriteCollectionKindProperty()
        {
            var property = new CodeProperty()
            {
                Description = "Additional data dictionary",
                Name = "additionalData",
                Kind = CodePropertyKind.AdditionalData,
                Access = AccessModifier.Private,
                Type = new CodeType() {Name = "array", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array}
            };
            parentClass.Kind = CodeClassKind.Model;
            parentClass.AddProperty(property);
            _refiner.Refine(root);
            propertyWriter.WriteCodeElement(property, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("private array $additionalData;", result);
            Assert.Contains("@var array<string, mixed>", result);
        }
        
        [Fact]
        public void WriteCollectionNonAdditionalData()
        {
            var property = new CodeProperty()
            {
                Name = "recipients",
                Kind = CodePropertyKind.Custom,
                Access = AccessModifier.Private,
                Type = new CodeType()
                {
                    Name = "recipient", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array
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
            var adapter = new CodeProperty()
            {
                Name = "adapter",
                Type = new CodeType() {Name = "requestAdapter", IsNullable = false},
                Access = AccessModifier.Private,
                Kind = CodePropertyKind.RequestAdapter
            };
            parentClass.AddProperty(adapter);
            parentClass.AddProperty(new CodeProperty()
            {
                Name = "pathSegment", 
                Kind = CodePropertyKind.PathParameters
            });
            propertyWriter.WriteCodeElement(adapter, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("private RequestAdapter $adapter;", result);
        }
        
        [Fact]
        public void WritePrimitiveFloatProperty()
        {
            CodeProperty property = new CodeProperty()
            {
                Name = "property",
                Type = new CodeType()
                {
                    Name = "double"
                },
                Access = AccessModifier.Protected
            };
            parentClass.AddProperty(property);
            propertyWriter.WriteCodeElement(property, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains($"protected ?float $property = null;", result);
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
    }
}
