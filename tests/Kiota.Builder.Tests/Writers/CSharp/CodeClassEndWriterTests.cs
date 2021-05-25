using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodeClassEndWriterTests: IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassEndWriter codeElementWriter;
        private readonly CodeClass parentClass;
        private const string propertyName = "propertyName";
        private const string propertyDescription = "some description";
        private const string typeName = "Somecustomtype";
        public CodeClassEndWriterTests() {
            codeElementWriter = new CodeClassEndWriter(new CSharpConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void ClosesNestedClasses() {
            var child = parentClass.AddInnerClass(new CodeClass(parentClass) {
                Name = "child"
            }).First();
            codeElementWriter.WriteCodeElement(child.EndBlock as CodeClass.End, writer);
            var result = tw.ToString();
            Assert.Equal(1, result.Count(x => x == '}'));
        }
        [Fact]
        public void ClosesNonNestedClasses() {
            codeElementWriter.WriteCodeElement(parentClass.EndBlock as CodeClass.End, writer);
            var result = tw.ToString();
            Assert.Equal(2, result.Count(x => x == '}'));
        }
    }
}
