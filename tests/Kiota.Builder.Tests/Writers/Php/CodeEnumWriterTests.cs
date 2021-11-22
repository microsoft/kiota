using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeEnumWriterTests :IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private const string EnumName = "someEnum";
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            root.Name = "Microsoft\\Graph";
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
            currentEnum.Options.Add(optionName);
            writer.Write(currentEnum);
            var result = tw.ToString();
            Assert.Contains("<?php", result);
            Assert.Contains("namespace Microsoft\\Graph;", result);
            Assert.Contains("use Microsoft\\Kiota\\Abstractions\\Enum", result);
            Assert.Contains($"class", result);
            Assert.Contains($"extends Enum", result);
            Assert.Contains($"public const {optionName.ToUpperInvariant()} = '{optionName}'", result);
            AssertExtensions.CurlyBracesAreClosed(result);
            Assert.Contains(optionName, result);
        }
    }
}



