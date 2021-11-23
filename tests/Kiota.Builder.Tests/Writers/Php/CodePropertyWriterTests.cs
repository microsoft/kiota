using System;
using System.IO;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Tests;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodePropertyWriterTests
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "Name";
        private readonly CodeClass parentClass;
        private readonly CodePropertyWriter propertyWriter;
        private readonly LanguageWriter writer;
        private readonly StringWriter tw;

        public CodePropertyWriterTests()
        {
            var root = CodeNamespace.InitRootNamespace();
            tw = new StringWriter();
            parentClass = new CodeClass()
            {
                Name = "ParentClass", Description = "This is an amazing class", ClassKind = CodeClassKind.Model
            };
            propertyWriter = new CodePropertyWriter(new PhpConventionService());
            root.AddClass(parentClass);
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            writer.SetTextWriter(tw);
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
            var cls = parentClass;
            cls.AddProperty(property);
            propertyWriter.WriteCodeElement(property, writer);

            var result = tw.ToString();
            Assert.Contains("@var EmailAddress|null $email", result);
            Assert.Contains("private ?EmailAddress $email;", result);
        }

        [Fact]
        public void WritePropertyRequestBuilder()
        {
            var property = new CodeProperty()
            {
                Name = "message",
                Access = AccessModifier.Public,
                Type = new CodeType()
                {
                    Name = "MessageRequestBuilder"
                },
                PropertyKind = CodePropertyKind.RequestBuilder
            };
            var cls = parentClass;
            cls.AddProperty(property);
            propertyWriter.WriteCodeElement(property, writer);

            var result = tw.ToString();
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
                PropertyKind = CodePropertyKind.AdditionalData,
                Access = AccessModifier.Private,
                Type = new CodeType() {Name = "array", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array}
            };
            var currentClass = parentClass;
            currentClass.ClassKind = CodeClassKind.Model;

            currentClass.AddProperty(property);
            
            propertyWriter.WriteCodeElement(property, writer);
            var result = tw.ToString();
            Assert.Contains("private array $additionalData;", result);
            Assert.Contains("@var array<string, mixed>|null", result);
        }
        
        [Fact]
        public void WriteCollectionNonAdditionalData()
        {
            var property = new CodeProperty()
            {
                Name = "recipients",
                PropertyKind = CodePropertyKind.Custom,
                Access = AccessModifier.Private,
                Type = new CodeType()
                {
                    Name = "recipient", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array
                }
            };
            var currentClass = parentClass;
            currentClass.AddProperty(property);
            
            propertyWriter.WriteCodeElement(property, writer);
            var result = tw.ToString();
            Assert.Contains("@var array<Recipient>|null", result);
        }

        [Fact]
        public void WriteRequestAdapter()
        {
            var currentClass = parentClass;
            var adapter = new CodeProperty()
            {
                Name = "adapter",
                Type = new CodeType() {Name = "requestAdapter", IsNullable = false},
                Access = AccessModifier.Private,
                PropertyKind = CodePropertyKind.RequestAdapter
            };
            
            
            currentClass.AddProperty(adapter);
            currentClass.AddProperty(new CodeProperty()
            {
                Name = "pathSegment", 
                PropertyKind = CodePropertyKind.PathParameters
            });
            propertyWriter.WriteCodeElement(adapter, writer);
            var result = tw.ToString();

            Assert.Contains("private RequestAdapter $adapter;", result);
        }
    }
}
