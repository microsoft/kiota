using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string enumName = "someEnum";
        private const string optionName = "Option1";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, defaultPath, defaultName);
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
            currentEnum.Options.Add(optionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("public enum", result);
            AssertExtensions.CurlyBracesAreClosed(result);
            Assert.Contains(optionName, result);
        }
        [Fact]
        public void WritesFlagsEnum() {
            currentEnum.Flags = true;
            currentEnum.Options.Add(optionName);
            currentEnum.Options.Add("option2");
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("[Flags]", result);
            Assert.Contains("= 1", result);
            Assert.Contains("= 2", result);
        }
        [Fact]
        public void DoesntWriteAnythingOnNoOption() {
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Empty(result);
        }
    }
}
