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
            codeElement.AddUsings(new CodeUsing()
            {
                Alias = string.Empty,
                Declaration = new CodeType()
                {
                    IsExternal = true
                },
                Name = "Microsoft\\Kiota\\Abstractions\\Enum",
                Parent = codeElement
            });
            if (codeElement?.Parent is CodeNamespace enumNamespace)
            {
                writer.WriteLine($"namespace {PhpConventionService.ReplaceDotsWithSlashInNamespaces(enumNamespace.Name)};");
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
            PhpConventionService.WriteCodeBlockEnd(writer);
        }
        
        private static string GetEnumValueName(string original)
        {
            return Regex.Replace(original, "([A-Z]{1})", "_$1").Trim('_').ToUpperInvariant();
        }
        
    }
}
