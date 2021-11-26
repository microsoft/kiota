using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeEnumWriterTests :IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeEnum currentEnum;
        private readonly ILanguageRefiner _languageRefiner;
        private const string EnumName = "someEnum";
        private readonly CodeEnumWriter _codeEnumWriter;
        public CodeEnumWriterTests(){
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            _languageRefiner = new PhpRefiner(new GenerationConfiguration {Language = GenerationLanguage.PHP});
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            root.Name = "Microsoft\\Graph";
            _codeEnumWriter = new CodeEnumWriter(new PhpConventionService());
            currentEnum = root.AddEnum(new CodeEnum {
                Name = EnumName,
            }).First();
        }
        public void Dispose(){
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        [Fact]
        public void WritesEnum()
        {
            var declaration = currentEnum.Parent as CodeNamespace;
            const string optionName = "option1";
            currentEnum.Options.Add(optionName);
            _languageRefiner.Refine(declaration);
            _codeEnumWriter.WriteCodeElement(currentEnum, writer);
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



