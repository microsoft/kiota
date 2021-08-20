using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, PhpConventionService>
    {
        public CodeEnumWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            conventions.WritePhpDocumentStart(writer);
            if (codeElement?.Parent is CodeNamespace enumNamespace)
            {
                writer.WriteLine($"namespace {enumNamespace.Name};");
            }
            writer.WriteLine();
            writer.WriteLine($"class {codeElement?.Name.ToFirstCharacterUpperCase()} {{");
            writer.IncreaseIndent();
            conventions.WriteCodeBlockEnd(writer);
        }
    }
}
