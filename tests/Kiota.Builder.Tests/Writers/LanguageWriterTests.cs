using System.ComponentModel;
using System.IO;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Writers.Tests {
    public class LanguageWriterTests {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        [Fact]
        public void GetCorrectWriterForLnaguage() {
            Assert.Equal(typeof(CSharpWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, defaultPath, defaultName).GetType());
            Assert.Equal(typeof(JavaWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, defaultPath, defaultName).GetType());
            Assert.Equal(typeof(RubyWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName).GetType());
            Assert.Equal(typeof(TypeScriptWriter),
                        LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName).GetType());
            Assert.Throws<InvalidEnumArgumentException>(() => LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, defaultPath, defaultName));
            Assert.Throws<InvalidEnumArgumentException>(() => LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, defaultPath, defaultName));
            Assert.Throws<InvalidEnumArgumentException>(() => LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, defaultPath, defaultName));
        }
    }
}
