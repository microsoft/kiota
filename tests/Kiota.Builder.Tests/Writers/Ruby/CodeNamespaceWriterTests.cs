using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeNamespaceWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeNamespaceWriter codeElementWriter;
        private readonly CodeNamespace parentNamespace;
        private readonly CodeNamespace childNamespace;

        public CodeNamespaceWriterTests() {
            codeElementWriter = new CodeNamespaceWriter(new RubyConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentNamespace = root.AddNamespace("parentNamespace");
            childNamespace = parentNamespace.AddNamespace("childNamespace");
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesSimpleDeclaration() {
            codeElementWriter.WriteCodeElement(childNamespace, writer);
            var result = tw.ToString();
            Assert.Contains("module", result);
            Assert.Single(Regex.Matches(result, ".*end.*"));
        }
    }
}
