using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string enumName = "someEnum";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, defaultPath, defaultName);
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
            Assert.Contains($"type {enumName.ToFirstCharacterUpperCase()} int", result);
            Assert.Contains($"const (", result);
            Assert.Contains($"{enumName.ToFirstCharacterUpperCase()} = iota", result);
            Assert.Contains($"func (i", result);
            Assert.Contains($"String() string {{", result);
            Assert.Contains($"return []string{{", result);
            Assert.Contains($"[i]", result);
            Assert.Contains($"func Parse", result);
            Assert.Contains($"(v string) (interface{{}}, error)", result);
            Assert.Contains($"switch v", result);
            Assert.Contains($"return 0, errors.New(\"Unknown ", result);
            AssertExtensions.CurlyBracesAreClosed(result);
            Assert.Contains(optionName.ToUpperInvariant(), result);
        }
        [Fact]
        public void DoesntWriteAnythingOnNoOption() {
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Empty(result);
        }
        [Fact]
        public void WritesUsing() {
            currentEnum.Usings.Add(new CodeUsing(currentEnum) {
                Name = "using1",
            });
            currentEnum.Options.Add("o");
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("using1", result);
        }
    }
}
