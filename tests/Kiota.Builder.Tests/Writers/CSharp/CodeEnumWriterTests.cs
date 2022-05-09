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
        private static readonly CodeEnumOption Option = new () {
            Name = "Option1",
        };
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
            currentEnum.AddOption(Option);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("public enum", result);
            AssertExtensions.CurlyBracesAreClosed(result, 1);
            Assert.Contains(Option.Name, result);
        }
        [Fact]
        public void WritesFlagsEnum() {
            currentEnum.Flags = true;
            currentEnum.AddOption(Option);
            currentEnum.AddOption(new CodeEnumOption { Name = "option2"});
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("[Flags]", result);
            Assert.Contains("= 1", result);
            Assert.Contains("= 2", result);
        }
        [Fact]
        public void WritesEnumOptionDescription() {
            Option.Description = "Some option description";
            currentEnum.AddOption(Option);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains($"<summary>{Option.Description}</summary>", result);
            AssertExtensions.CurlyBracesAreClosed(result, 1);
        }
        [Fact]
        public void DoesntWriteAnythingOnNoOption() {
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Empty(result);
        }
    }
}
