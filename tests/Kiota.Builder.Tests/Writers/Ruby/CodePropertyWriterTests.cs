using System;
using System.IO;
using Kiota.Builder.Extensions;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private readonly CodeClass EmptyClass;
        private readonly CodeProperty emptyProperty;
        private const string PropertyName = "propertyName";
        private const string TypeName = "Somecustomtype";
        private const string RootNamespaceName = "RootNamespace";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var emptyRoot = CodeNamespace.InitRootNamespace();
            EmptyClass = new CodeClass {
                Name = "emptyClass"
            };
            emptyProperty = new CodeProperty {
                Name = PropertyName,
            };
            emptyProperty.Type = new CodeType {
                Name = TypeName
            };
            EmptyClass.AddProperty(emptyProperty, new() {
                Name = "pathParameters",
                Kind = CodePropertyKind.PathParameters,
            }, new() {
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
            });
            
            var root = CodeNamespace.InitRootNamespace();
            root.Name = RootNamespaceName;
            parentClass = new CodeClass {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            property = new CodeProperty {
                Name = PropertyName,
            };
            property.Type = new CodeType {
                Name = TypeName,
                TypeDefinition = parentClass
            };
            parentClass.AddProperty(property, new() {
                Name = "pathParameters",
                Kind = CodePropertyKind.PathParameters,
            }, new() {
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
            });
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesRequestBuilder() {
            property.Kind = CodePropertyKind.RequestBuilder;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"def {PropertyName.ToSnakeCase()}", result);
            Assert.Contains($"{RootNamespaceName}::{TypeName}.new", result);
            Assert.Contains("request_adapter", result);
            Assert.Contains("path_parameters", result);
        }
        [Fact]
        public void WritesRequestBuilderWithoutNamespace() {
            emptyProperty.Kind = CodePropertyKind.RequestBuilder;
            writer.Write(emptyProperty);
            var result = tw.ToString();
            Assert.Contains($"def {PropertyName.ToSnakeCase()}", result);
            Assert.Contains($"{TypeName}.new", result);
            Assert.Contains("request_adapter", result);
            Assert.Contains("path_parameters", result);
            Assert.DoesNotContain($"::{TypeName}.new", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.Kind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"@{PropertyName.ToSnakeCase()}", result);
        }
    }
}
