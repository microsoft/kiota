using System;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.TypeScript {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, TypeScriptConventionService>
    {
        public CodePropertyWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var returnType = conventions.GetTypeString(codeElement.Type);
            var isFlagEnum = codeElement.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum currentEnum && currentEnum.Flags;
            conventions.WriteShortDescription(codeElement.Description, writer);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} get {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                default:
                    var defaultValue = string.IsNullOrEmpty(codeElement.DefaultValue) ? string.Empty : $" = {codeElement.DefaultValue}";
                    var singleLiner = codeElement.PropertyKind == CodePropertyKind.Custom || codeElement.PropertyKind == CodePropertyKind.AdditionalData;
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{(codeElement.ReadOnly ? " readonly ": " ")}{codeElement.Name.ToFirstCharacterLowerCase()}{(codeElement.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(isFlagEnum ? "[]" : string.Empty)}{(codeElement.Type.IsNullable ? " | undefined" : string.Empty)}{defaultValue}{(singleLiner ? ";" : string.Empty)}");
                break;
            }
        }
    }
}
