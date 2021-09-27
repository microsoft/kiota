using System.ComponentModel;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Writers.Tests {
    public class LanguageWriterTests {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        [Fact]
        public void GetCorrectWriterForLanguage() {
            Assert.Equal(typeof(CSharpWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName).GetType());
            Assert.Equal(typeof(JavaWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName).GetType());
            Assert.Equal(typeof(RubyWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName).GetType());
            Assert.Equal(typeof(TypeScriptWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName).GetType());
            Assert.Equal(typeof(GoWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName).GetType());
            Assert.Throws<InvalidEnumArgumentException>(() => LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName));
            Assert.Throws<InvalidEnumArgumentException>(() => LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName));
        }
    }
}
