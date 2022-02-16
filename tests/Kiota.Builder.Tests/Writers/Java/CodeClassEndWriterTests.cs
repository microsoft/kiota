using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodeClassEndWriterTests: IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassEndWriter codeElementWriter;
        private readonly CodeClass parentClass;
        public CodeClassEndWriterTests() {
            codeElementWriter = new CodeClassEndWriter();
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
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
        public void ClosesNestedClasses() {
            var child = parentClass.AddInnerClass(new CodeClass {
                Name = "child"
            }).First();
            codeElementWriter.WriteCodeElement(child.EndBlock as CodeClass.ClassEnd, writer);
            var result = tw.ToString();
            Assert.Equal(1, result.Count(x => x == '}'));
        }
        [Fact]
        public void ClosesNonNestedClasses() {
            codeElementWriter.WriteCodeElement(parentClass.EndBlock as CodeClass.ClassEnd, writer);
            var result = tw.ToString();
            Assert.Equal(1, result.Count(x => x == '}'));
        }
    }
}
