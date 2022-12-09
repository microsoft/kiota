using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, TypeScriptConventionService>
{
    public CodePropertyWriter(TypeScriptConventionService conventionService) : base(conventionService){}
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
        var isFlagEnum = codeElement.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum currentEnum && currentEnum.Flags;
        var parentClass = codeElement.Parent as CodeClass;
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        switch(codeElement.Kind) {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} get {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                writer.IncreaseIndent();
                conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                writer.DecreaseIndent();
                writer.WriteLine("}");
            break;
            default:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {codeElement.NamePrefix}{codeElement.Name.ToFirstCharacterLowerCase()}{(codeElement.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(isFlagEnum ? "[]" : string.Empty)}{(codeElement.Type.IsNullable ? " | undefined" : string.Empty)};");
            break;
        }
    }
}
