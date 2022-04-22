﻿using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.Php.Tests
{
    public class CodeClassEndWriterTests: IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeBlockEndWriter codeElementWriter;
        private readonly CodeClass parentClass;
        public CodeClassEndWriterTests() {
            codeElementWriter = new CodeBlockEndWriter();
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
        }
        public void Dispose() {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        
        [Fact]
        public void ClosesNonNestedClasses() {
            codeElementWriter.WriteCodeElement(parentClass.EndBlock as BlockEnd, writer);
            var result = tw.ToString();
            Assert.Equal(1, result.Count(x => x == '}'));
        }
    }
}
