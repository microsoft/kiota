using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
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
        private const string end = "end";
        public CodeClassEndWriterTests() {
            codeElementWriter = new CodeClassEndWriter(new RubyConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
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
            Assert.Single(Regex.Matches(result, ".*end.*"));
        }
        [Fact]
        public void ClosesNonNestedClasses() {
            codeElementWriter.WriteCodeElement(parentClass.EndBlock as CodeClass.End, writer);
            var result = tw.ToString();
            Assert.Equal(2, Regex.Matches(result, ".*end.*").Count());
        }
    }
}
