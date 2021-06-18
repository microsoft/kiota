using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests {
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;

        public CodeClassDeclarationWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName);
            codeElementWriter = new CodeClassDeclarationWriter(new TypeScriptConventionService(writer));
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new (root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesSimpleDeclaration() {
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as CodeClass.Declaration, writer);
            var result = tw.ToString();
            Assert.Contains("export class", result);
        }
        [Fact]
        public void WritesImplementation() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Implements.Add(new (parentClass){
                Name = "someInterface"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("implements", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Inherits = new (parentClass){
                Name = "someInterface"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("extends", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Usings.Add(new (parentClass) {
                Name = "Objects",
                Declaration = new(parentClass) {
                    Name = "util",
                    IsExternal = true,
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("import", result);
            Assert.Contains("from", result);
            Assert.Contains("'util'", result);
        }
    }
}
