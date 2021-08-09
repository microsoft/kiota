using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private const string propertyName = "propertyName";
        private const string propertyDescription = "some description";
        private const string typeName = "Somecustomtype";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            property = new CodeProperty(parentClass) {
                Name = propertyName,
            };
            property.Type = new CodeType(property) {
                Name = typeName
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
            Assert.Contains($"return new {typeName}", result);
            Assert.Contains("this.httpCore", result);
            Assert.Contains("this.pathSegment", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"{propertyName}?: {typeName} | undefined", result);
        }
        [Fact]
        public void WritesPrivateSetter() {
            property.PropertyKind = CodePropertyKind.Custom;
            property.ReadOnly = true;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("readonly", result);
        }
        [Fact]
        public void WritesFlagEnums() {
            property.PropertyKind = CodePropertyKind.Custom;
            property.Type = new CodeType(property) {
                Name = "customEnum",
            };
            (property.Type as CodeType).TypeDefinition = new CodeEnum(property.Type) {
                Name = "customEnumType",
                Flags = true,
            };
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("[]", result);
        }
    }
}
