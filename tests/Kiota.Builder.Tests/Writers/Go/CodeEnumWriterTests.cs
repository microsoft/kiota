using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go {
    public class CodeEnumWriterTests :IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string EnumName = "someEnum";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
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
            const string optionName = "option1";
            currentEnum.AddOption(new CodeEnumOption { Name = optionName});
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains($"type {EnumName.ToFirstCharacterUpperCase()} int", result);
            Assert.Contains("const (", result);
            Assert.Contains($"{EnumName.ToFirstCharacterUpperCase()} = iota", result);
            Assert.Contains("func (i", result);
            Assert.Contains("String() string {", result);
            Assert.Contains("return []string{", result);
            Assert.Contains("[i]", result);
            Assert.Contains("func Parse", result);
            Assert.Contains("(v string) (interface{}, error)", result);
            Assert.Contains("switch v", result);
            Assert.Contains("default", result);
            Assert.Contains("result :=", result);
            Assert.Contains("return &result, nil", result);
            Assert.Contains("return 0, errors.New(\"Unknown ", result);
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
            currentEnum.AddUsing(new CodeUsing {
                Name = "using1",
            });
            currentEnum.AddOption(new CodeEnumOption{ Name = "o"});
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("using1", result);
        }
        [Fact]
        public void WritesEnumOptionDescription() {
            var option = new CodeEnumOption {
                Documentation = new() {
                    Description = "Some option description",
                },
                Name = "option1",
            };
            currentEnum.AddOption(option);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains($"// {option.Documentation.Description}", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
    }
}
