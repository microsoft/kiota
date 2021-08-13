using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private readonly CodeClass EmptyClass;
        private readonly CodeProperty emptyProperty;
        private const string propertyName = "propertyName";
        private const string propertyDescription = "some description";
        private const string typeName = "Somecustomtype";
        private const string rootNamespaceName = "RootNamespace";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var emptyRoot = CodeNamespace.InitRootNamespace();
            EmptyClass = new CodeClass(emptyRoot) {
                Name = "emptyClass"
            };
            emptyProperty = new CodeProperty(EmptyClass) {
                Name = propertyName,
            };
            emptyProperty.Type = new CodeType(emptyProperty) {
                Name = typeName
            };
            EmptyClass.AddProperty(emptyProperty);
            
            var root = CodeNamespace.InitRootNamespace();
            root.Name = rootNamespaceName;
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            property = new CodeProperty(parentClass) {
                Name = propertyName,
            };
            property.Type = new CodeType(property) {
                Name = typeName,
                TypeDefinition = parentClass
            };
            parentClass.AddProperty(property);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesRequestBuilder() {
            property.PropertyKind = CodePropertyKind.RequestBuilder;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"def {propertyName.ToSnakeCase()}", result);
            Assert.Contains($"{rootNamespaceName}::{typeName}.new", result);
            Assert.Contains("http_core", result);
            Assert.Contains("path_segment", result);
        }
        [Fact]
        public void WritesRequestBuilderWithoutNamespace() {
            emptyProperty.PropertyKind = CodePropertyKind.RequestBuilder;
            writer.Write(emptyProperty);
            var result = tw.ToString();
            Assert.Contains($"def {propertyName.ToSnakeCase()}", result);
            Assert.Contains($"{typeName}.new", result);
            Assert.Contains("http_core", result);
            Assert.Contains("path_segment", result);
            Assert.DoesNotContain($"::{typeName}.new", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"@{propertyName.ToSnakeCase()}", result);
        }
    }
}
