using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string enumName = "someEnum";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            currentEnum = root.AddEnum(new CodeEnum(root) {
                Name = enumName,
            }).First();
        }
        public void Dispose(){
            tw?.Dispose();
        }
        [Fact]
        public void WritesEnum() {
            const string optionName = "option1";
            currentEnum.Options.Add(optionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains($"public enum", result);
            Assert.Contains($"implements ValuedEnum", result);
            Assert.Contains($"public final String value", result);
            Assert.Contains($"this.value = value", result);
            Assert.Contains($"public String getValue()", result);
            Assert.Contains($"return this.value", result);
            Assert.Contains($"@javax.annotation.Nonnull", result);
            Assert.Contains($"@javax.annotation.Nullable", result);
            Assert.Contains($"forValue(@javax.annotation.Nonnull final String searchValue)", result);
            Assert.Contains($"default: return null;", result);
            AssertExtensions.CurlyBracesAreClosed(result);
            Assert.Contains(optionName, result);
        }
        [Fact]
        public void DoesntWriteAnythingOnNoOption() {
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Empty(result);
        }
    }
}
