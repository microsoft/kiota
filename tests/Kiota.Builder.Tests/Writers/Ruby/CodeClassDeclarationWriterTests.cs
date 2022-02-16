using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;

        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new RubyConventionService(), "graph");
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
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
            Assert.Contains("class", result);
        }
        [Fact]
        public void WritesImplementation() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            declaration.AddImplements(new CodeType {
                Name = "someInterface"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("include", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            declaration.Inherits = new (){
                Name = "someInterface"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("<", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            var messageClass = parentClass.GetImmediateParentOfType<CodeNamespace>().AddClass( new CodeClass {
                Name = "Message"
            }).First();
            declaration.AddUsings(new () {
                Name = "Objects",
                Declaration = new() {
                    Name = "util",
                    IsExternal = true,
                }
            },
            new () {
                Name = "project-graph",
                Declaration = new() {
                    Name = "Message",
                    TypeDefinition = messageClass,
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("require_relative", result);
            Assert.Contains("message", result);
            Assert.Contains("require", result);
        }
        [Fact]
        public void WritesMixins() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            declaration.AddImplements(new CodeType {
                Name = "test"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("include", result);
            Assert.Contains("test", result);
        }
    }
}
