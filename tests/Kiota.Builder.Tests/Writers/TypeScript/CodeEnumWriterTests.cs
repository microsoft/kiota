using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests {
    public class CodeEnumWriterTests :IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string enumName = "someEnum";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName);
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
            Assert.Contains($"export enum", result);
            Assert.Contains(optionName, result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
    }
}
