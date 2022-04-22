﻿using System;
using System.IO;
using Kiota.Builder.Refiners;
using Xunit;

namespace Kiota.Builder.Writers.Php.Tests
{
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;
        private readonly ILanguageRefiner _refiner;
        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new PhpConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            _refiner = new PhpRefiner(new GenerationConfiguration() {Language = GenerationLanguage.PHP});
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
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as ClassDeclaration, writer);
            var result = tw.ToString();
            Assert.Contains("class ParentClass", result);
        }
        [Fact]
        public void WritesImplementation() {
            var declaration = parentClass.StartBlock as ClassDeclaration;
            declaration.AddImplements(new CodeType {
                Name = "\\Stringable"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("implements \\Stringable", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as ClassDeclaration;
            declaration.Inherits = new (){
                Name = "someInterface"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("extends", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as ClassDeclaration;
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
            var declaration = parentClass.StartBlock as ClassDeclaration;
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
                Kind = CodeMethodKind.RequestExecutor,
                HttpMethod = HttpMethod.Get,
                ReturnType = new CodeType
                {
                    Name = "Promise",
                    Parent = declaration
                }
            });
            var dec = declaration?.StartBlock as ClassDeclaration;
            var namespaces = declaration?.Parent as CodeNamespace;
            _refiner.Refine(namespaces);
            codeElementWriter.WriteCodeElement(dec, writer);
            var result = tw.ToString();
            
            Assert.Contains("use Http\\Promise\\Promise;", result);
            Assert.Contains("use Http\\Promise\\RejectedPromise;", result);
            Assert.Contains("use Exception;", result);
        }

        [Fact]
        public void ExtendABaseClass()
        {
            var currentClass = parentClass.StartBlock as ClassDeclaration;
            if (currentClass != null)
            {
                currentClass.Inherits = new CodeType()
                {
                    TypeDefinition = new CodeClass() {Name = "Model", Kind = CodeClassKind.Custom}
                };
            }

            codeElementWriter.WriteCodeElement(currentClass, writer);
            var result = tw.ToString();
            Assert.Contains("extends", result);

        }
    }
}
