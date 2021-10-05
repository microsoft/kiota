using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string EnumName = "someEnum";
        private const string OptionName = "Option1";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            currentEnum = root.AddEnum(new CodeEnum {
                Name = EnumName,
            }).First();
        }
        public void Dispose(){
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesEnum() {
            currentEnum.Options.Add(OptionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("public enum", result);
            AssertExtensions.CurlyBracesAreClosed(result);
            Assert.Contains(OptionName, result);
        }
        [Fact]
        public void WritesFlagsEnum() {
            currentEnum.Flags = true;
            currentEnum.Options.Add(OptionName);
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
