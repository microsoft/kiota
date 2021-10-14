using System;
using System.IO;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private const string PropertyName = "PropertyName";
        private const string TypeName = "Somecustomtype";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
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
            parentClass.AddProperty(property);
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
            Assert.Contains("get =>", result);
            Assert.Contains($"new {TypeName}", result);
            Assert.Contains("RequestAdapter", result);
            Assert.Contains("PathSegment", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"{TypeName} {PropertyName}", result);
            Assert.Contains("get; set;", result);
        }
        [Fact]
        public void WritesPrivateSetter() {
            property.PropertyKind = CodePropertyKind.Custom;
            property.ReadOnly = true;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get; private set;", result);
        }
        [Fact]
        public void MapsCustomPropertiesToBackingStore() {
            parentClass.AddBackingStoreProperty();
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(nameof(PropertyName)); }", result);
            Assert.Contains("set { BackingStore?.Set(nameof(PropertyName), value);", result);
        }
        [Fact]
        public void MapsAdditionalDataPropertiesToBackingStore() {
            parentClass.AddBackingStoreProperty();
            property.PropertyKind = CodePropertyKind.AdditionalData;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(nameof(PropertyName)); }", result);
            Assert.Contains("set { BackingStore?.Set(nameof(PropertyName), value);", result);
        }
    }
}
