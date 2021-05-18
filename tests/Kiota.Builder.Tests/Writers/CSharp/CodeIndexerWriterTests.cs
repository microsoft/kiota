using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodeIndexerWriterTests : IDisposable
    {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClass parentClass;
        private readonly CodeIndexer indexer;
        public CodeIndexerWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            indexer = new CodeIndexer(parentClass) {
                Name = "idx",
            };
            indexer.IndexType = new CodeType(indexer) {
                Name = "string",
            };
            indexer.ReturnType = new CodeType(indexer) {
                Name = "SomeRequestBuilder"
            };
            parentClass.SetIndexer(indexer);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesIndexer() {
            writer.Write(indexer);
            var result = tw.ToString();
            Assert.Contains("HttpCore = HttpCore", result);
            Assert.Contains("SerializerFactory = SerializerFactory", result);
            Assert.Contains("CurrentPath = CurrentPath + PathSegment", result);
            Assert.Contains("+ position", result);
            Assert.Contains("public SomeRequestBuilder this[string position]", result);
        }
    }
}
