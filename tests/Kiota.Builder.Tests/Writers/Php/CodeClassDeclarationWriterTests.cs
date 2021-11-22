using System;
using System.IO;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;

        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new PhpConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            root.Name = "Microsoft\\Graph";
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
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as CodeClass.Declaration, writer);
            var result = tw.ToString();
            Assert.Contains("class ParentClass", result);
        }
        [Fact]
        public void WritesImplementation() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.AddImplements(new CodeType {
                Name = "\\Stringable"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("implements \\Stringable", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Inherits = new (){
                Name = "someInterface"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("extends", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.AddUsings(new () {
                Name = "Promise",
                Declaration = new() {
                    Name = "Http\\Promise\\",
                    IsExternal = true,
                }
            },
            new () {
                Name = "Microsoft\\Graph\\Models",
                Declaration = new() {
                    Name = "Message",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("use Microsoft\\Graph\\Models\\Message", result);
            Assert.Contains("use Http\\Promise\\Promise", result);
        }
        [Fact]
        public void RemovesImportWithClassName() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.AddUsings(new CodeUsing {
                Name = "Microsoft\\Graph\\Models",
                Declaration = new() {
                    Name = "ParentClass",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.DoesNotContain("Microsoft\\Graph\\Models\\ParentClass", result);
        }

        [Fact]
        public void ImportRequiredClassesWhenContainsRequestExecutor()
        {
            var declaration = parentClass;
            declaration?.AddMethod(new CodeMethod()
            {
                Name = "get",
                Access = AccessModifier.Public,
                MethodKind = CodeMethodKind.RequestExecutor,
                HttpMethod = HttpMethod.Get,
                ReturnType = new CodeType
                {
                    Name = "Promise",
                    Parent = declaration
                }
            });
            var dec = declaration?.StartBlock as CodeClass.Declaration;
            codeElementWriter.WriteCodeElement(dec, writer);
            var result = tw.ToString();
            Assert.Contains("use Http\\Promise\\Promise;", result);
            Assert.Contains("use Http\\Promise\\RejectedPromise;", result);
            Assert.Contains("use \\Exception;", result);
        }

        [Fact]
        public void ExtendABaseClass()
        {
            var currentClass = parentClass.StartBlock as CodeClass.Declaration;
            if (currentClass != null)
            {
                currentClass.Inherits = new CodeType()
                {
                    TypeDefinition = new CodeClass() {Name = "Model", ClassKind = CodeClassKind.Custom}
                };
            }

            codeElementWriter.WriteCodeElement(currentClass, writer);
            var result = tw.ToString();
            Assert.Contains("extends", result);

        }
    }
}
