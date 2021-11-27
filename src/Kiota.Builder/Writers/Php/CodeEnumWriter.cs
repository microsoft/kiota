using System.Text.RegularExpressions;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, PhpConventionService>
    {
        public CodeEnumWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            PhpConventionService.WritePhpDocumentStart(writer);
            var enumProperties = codeElement.Options;
            if (codeElement?.Parent is CodeNamespace enumNamespace)
            {
                writer.WriteLine($"namespace {enumNamespace.Name.ReplaceDotsWithSlashInNamespaces()};");
            }
            writer.WriteLine();

            foreach (var use in codeElement.Usings)
            {
                    writer.WriteLine($"use {use.Name};");
            }
            writer.WriteLine();
            writer.WriteLine($"class {codeElement?.Name.ToFirstCharacterUpperCase()} extends Enum {{");
            writer.IncreaseIndent();
            foreach (var enumProperty     in enumProperties)
            {
                writer.WriteLine($"public const {GetEnumValueName(enumProperty)} = '{enumProperty}';");
            }
            writer.CloseBlock();
        }
        
        private static string GetEnumValueName(string original)
        {
            return Regex.Replace(original, "([A-Z]{1})", "_$1").Trim('_').ToUpperInvariant();
        }
        
    }
}
