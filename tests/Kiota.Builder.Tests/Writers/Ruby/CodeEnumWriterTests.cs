using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string enumName = "someEnum";
        private readonly CodeNamespace parentNamespace;
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentNamespace = root.AddNamespace("parentNamespace");
            currentEnum = parentNamespace.AddEnum(new CodeEnum(root) {
            Name = enumName,
            }).First();
        }
        public void Dispose(){
            tw?.Dispose();
        }
        [Fact]
        public void WritesEnum() {
            var module = currentEnum?.Parent?.Parent as CodeNamespace;
            module.Name = "testModule";
            const string optionName = "Option1";
            currentEnum.Options.Add(optionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains($"= {{", result);
            Assert.Contains(optionName, result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void DoesntWriteAnythingOnNoOption() {
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Empty(result);
        }
        [Fact]
        public void WritesModule() {
            var module = currentEnum?.Parent as CodeNamespace;
            module.Name = "testModule";
            const string optionName = "Option2";
            currentEnum.Options.Add(optionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("module TestModule", result);
            Assert.Contains(":Option2", result);
        }
    }
}
