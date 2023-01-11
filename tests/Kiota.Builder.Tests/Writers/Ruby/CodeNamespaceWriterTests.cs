using System;
using System.IO;
using System.Text.RegularExpressions;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Ruby;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Ruby {
    public class CodeNamespaceWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private const string ClientNamespaceName = "graph";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeNamespaceWriter codeElementWriter;
        private readonly CodeNamespace parentNamespace;
        private readonly CodeNamespace childNamespace;

        public CodeNamespaceWriterTests() {
            codeElementWriter = new CodeNamespaceWriter(new RubyConventionService(), new(Path.GetTempFileName(), ClientNamespaceName));
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
