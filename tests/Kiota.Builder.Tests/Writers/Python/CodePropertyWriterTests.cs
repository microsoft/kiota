using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.Python.Tests {
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
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
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
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
            });
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesQueryParameters() {
            property.Kind = CodePropertyKind.QueryParameters;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"property_name: Optional[somecustomtype.Somecustomtype]", result);
        }
        [Fact]
        public void WritesDefaultValuesForProperties() {
            property.Kind = CodePropertyKind.Headers;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("= None", result);
        }
    }
}
