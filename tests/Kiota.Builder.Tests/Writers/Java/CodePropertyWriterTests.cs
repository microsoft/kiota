using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private const string PropertyName = "propertyName";
        private const string TypeName = "Somecustomtype";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            property = new CodeProperty {
                Name = PropertyName,
            };
            property.Type = new CodeType {
                Name = TypeName
            };
            parentClass.AddProperty(property, new() {
                Name = "pathParameters",
                PropertyKind = CodePropertyKind.PathParameters,
            }, new() {
                Name = "requestAdapter",
                PropertyKind = CodePropertyKind.RequestAdapter,
            });
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesRequestBuilder() {
            property.PropertyKind = CodePropertyKind.RequestBuilder;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"return new {TypeName}", result);
            Assert.Contains("requestAdapter", result);
            Assert.Contains("pathParameters", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"{TypeName} {PropertyName}", result);
            Assert.Contains("@javax.annotation.Nullable", result);
        }
        [Fact]
        public void WritesFlagEnums() {
            property.PropertyKind = CodePropertyKind.Custom;
            property.Type = new CodeType {
                Name = "customEnum",
            };
            (property.Type as CodeType).TypeDefinition = new CodeEnum {
                Name = "customEnumType",
                Flags = true,
            };
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("EnumSet", result);
        }
        [Fact]
        public void WritesNonNull() {
            property.PropertyKind = CodePropertyKind.Custom;
            (property.Type as CodeType).IsNullable = false;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("@javax.annotation.Nonnull", result);
        }
    }
}
