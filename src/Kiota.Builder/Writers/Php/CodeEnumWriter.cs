using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, PhpConventionService>
    {
        public CodeEnumWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            conventions.WritePhpDocumentStart(writer);
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()} {{");
            writer.IncreaseIndent();
            conventions.WriteCodeBlockEnd(writer);
        }
    }
}
