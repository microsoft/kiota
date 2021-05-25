using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Tests {
    public class CodeTypeWriterTests: IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeType currentType;
        private const string typeName = "SomeType";
        public CodeTypeWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            currentType = new (root) {
                Name = typeName
            };
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesCodeType() {
            writer.Write(currentType);
            var result = tw.ToString();
            Assert.Contains(typeName, result);
        }
    }
}
