using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeNamespaceWriterTests : IDisposable
    {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeNamespaceWriter codeElementWriter;
        private readonly CodeNamespace parentNamespace;
        private readonly CodeNamespace childNamespace;

        public CodeNamespaceWriterTests() {
            codeElementWriter = new CodeNamespaceWriter(new RubyConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentNamespace = root.AddNamespace("parentNamespace");
            childNamespace = parentNamespace.AddNamespace("childNamespace");
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesSimpleDeclaration() {
            codeElementWriter.WriteCodeElement(childNamespace, writer);
            var result = tw.ToString();
            Assert.Contains("module", result);
            Assert.Equal(1, Regex.Matches(result, ".*end.*").Count());
        }
        // [Fact]
        // public void WritesImplementation() {
        //     var declaration = parentClass.StartBlock as CodeClass.Declaration;
        //     declaration.Implements.Add(new (parentClass){
        //         Name = "someInterface"
        //     });
        //     codeElementWriter.WriteCodeElement(declaration, writer);
        //     var result = tw.ToString();
        //     Assert.Contains("include", result);
        // }
        // [Fact]
        // public void WritesInheritance() {
        //     var declaration = parentClass.StartBlock as CodeClass.Declaration;
        //     declaration.Inherits = new (parentClass){
        //         Name = "someInterface"
        //     };
        //     codeElementWriter.WriteCodeElement(declaration, writer);
        //     var result = tw.ToString();
        //     Assert.Contains("<", result);
        // }
        // [Fact]
        // public void WritesImports() {
        //     var declaration = parentClass.StartBlock as CodeClass.Declaration;
        //     declaration.Usings.Add(new (parentClass) {
        //         Name = "Objects",
        //         Declaration = new(parentClass) {
        //             Name = "util",
        //             IsExternal = true,
        //         }
        //     });
        //     declaration.Usings.Add(new (parentClass) {
        //         Name = "project-graph",
        //         Declaration = new(parentClass) {
        //             Name = "Message",
        //         }
        //     });
        //     codeElementWriter.WriteCodeElement(declaration, writer);
        //     var result = tw.ToString();
        //     Assert.Contains("require_relative", result);
        //     Assert.Contains("message", result);
        //     Assert.Contains("require", result);
        // }
        // [Fact]
        // public void WritesMixins() {
        //     var declaration = parentClass.StartBlock as CodeClass.Declaration;
        //     declaration.Implements.Add(new (parentClass) {
        //         Name = "test"
        //     });
        //     codeElementWriter.WriteCodeElement(declaration, writer);
        //     var result = tw.ToString();
        //     Assert.Contains("include", result);
        //     Assert.Contains("test", result);
        // }
    }
}
