using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;

        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new JavaConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
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
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesSimpleDeclaration() {
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as CodeClass.Declaration, writer);
            var result = tw.ToString();
            Assert.Contains("public class", result);
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
                    Name = "java.util",
                    IsExternal = true,
                }
            });
            declaration.Usings.Add(new (parentClass) {
                Name = "project.graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("import project.graph.Message", result);
            Assert.Contains("import java.util.Objects", result);
        }
        [Fact]
        public void RemovesImportWithClassName() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Usings.Add(new (parentClass) {
                Name = "project.graph",
                Declaration = new(parentClass) {
                    Name = "parentClass",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.DoesNotContain("project.graph.parentClass", result);
        }
    }
}
