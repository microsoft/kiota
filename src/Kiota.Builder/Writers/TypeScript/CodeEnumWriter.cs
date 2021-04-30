using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.TypeScript {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, TypeScriptConventionService>
    {
        public CodeEnumWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"export enum {codeElement.Name.ToFirstCharacterUpperCase()} {{");
            writer.IncreaseIndent();
            codeElement.Options.ForEach(x => writer.WriteLine($"{x.ToFirstCharacterUpperCase()} = \"{x}\","));
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
