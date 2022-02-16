using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;
        private readonly CodeNamespace root;

        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new GoConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            root = CodeNamespace.InitRootNamespace();
            parentClass = new () {
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
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as CodeClass.ClassDeclaration, writer);
            var result = tw.ToString();
            Assert.Contains("type", result);
            Assert.Contains("struct", result);
            Assert.Contains("package", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            declaration.Inherits = new (){
                Name = "someParent"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("SomeParent", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            declaration.AddUsings(new () {
                Name = "Objects",
                Declaration = new() {
                    Name = "Go.util",
                    IsExternal = true,
                }
            },
            new () {
                Name = "project.graph",
                Declaration = new() {
                    Name = "Message",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("import (", result);
            Assert.Contains("ib040b76fd8c2f056725723d35361c053f34e28d2ff1828ce830f3e00e807a59b \"project/graph\"", result);
            Assert.Contains("i5bda920e7ace9d5445a4ab998b248741f78d1219c76fbec57ddf13651f485ee4 \"Go.util\"", result);
        }
    }
}
