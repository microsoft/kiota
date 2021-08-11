using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
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
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, defaultPath, defaultName);
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
            Assert.Throws<InvalidOperationException>(() => writer.Write(property));
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"{propertyName.ToFirstCharacterLowerCase()} *{typeName}", result);
        }
        // [Fact]
        // public void WritesFlagEnums() {
        //     property.PropertyKind = CodePropertyKind.Custom;
        //     property.Type = new CodeType(property) {
        //         Name = "customEnum",
        //     };
        //     (property.Type as CodeType).TypeDefinition = new CodeEnum(property.Type) {
        //         Name = "customEnumType",
        //         Flags = true,
        //     };
        //     writer.Write(property);
        //     var result = tw.ToString();
        //     Assert.Contains("EnumSet", result);
        // }
        //TODO: flag enum support needs to be added in Go
        [Fact]
        public void WritesNonNull() {
            property.PropertyKind = CodePropertyKind.Custom;
            (property.Type as CodeType).IsNullable = false;
            writer.Write(property);
            var result = tw.ToString();
            Assert.DoesNotContain("*", result);
        }
    }
}
