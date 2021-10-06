using System;
using System.IO;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodeIndexerWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClass parentClass;
        private readonly CodeIndexer indexer;
        public CodeIndexerWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            indexer = new CodeIndexer {
                Name = "idx",
            };
            indexer.IndexType = new CodeType {
                Name = "string",
            };
            indexer.ReturnType = new CodeType {
                Name = "SomeRequestBuilder"
            };
            parentClass.SetIndexer(indexer);
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesIndexer() {
            writer.Write(indexer);
            var result = tw.ToString();
            Assert.Contains("RequestAdapter", result);
            Assert.Contains("PathSegment", result);
            Assert.Contains("+ position", result);
            Assert.Contains("public SomeRequestBuilder this[string position]", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
    }
}
