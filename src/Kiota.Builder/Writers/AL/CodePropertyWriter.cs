using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.AL;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, ALConventionService>
{
    public CodePropertyWriter(ALConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ParentIsSkipped()) return;
        if (codeElement.IsGlobalVariable()) return;
        if (codeElement.IsObjectProperty()) return;
        if (codeElement.ExistsInExternalBaseType) return;
        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        //conventions.WriteShortDescription(codeElement, writer);
        //conventions.WriteDeprecationAttribute(codeElement, writer);

        WritePropertyInternal(codeElement, writer, propertyType);// Always write the normal way
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        var simpleBody = $"get; {setterAccessModifier}set;";
        var defaultValue = string.Empty;
        switch (codeElement.Kind)
        {
            // case CodePropertyKind.RequestBuilder:
            //     writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{propertyType} {codeElement.Name.ToFirstCharacterUpperCase()}");
            //     writer.StartBlock();
            //     writer.Write("get => ");
            //     conventions.AddRequestBuilderBody(parentClass, propertyType, writer, includeIndent: false);
            //     writer.CloseBlock();
            //     break;
            // case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            // case CodePropertyKind.Custom when backingStoreProperty != null:
            //     var backingStoreKey = codeElement.WireName;
            //     var nullableOp = !codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? "?" : string.Empty;
            //     var defaultPropertyValue = codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? " ?? new Dictionary<string, object>()" : string.Empty;
            //     writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{propertyType} {codeElement.Name.ToFirstCharacterUpperCase()}");
            //     writer.StartBlock();
            //     writer.WriteLine($"get {{ return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Get<{propertyType}>(\"{backingStoreKey}\"){defaultPropertyValue}; }}");
            //     writer.WriteLine($"set {{ {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Set(\"{backingStoreKey}\", value); }}");
            //     writer.CloseBlock();
            //     break;
            // case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
            //     if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(), static x => x.Name.ToFirstCharacterUpperCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
            //         writer.WriteLine($"public override {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get => {primaryMessageCodePath} ?? string.Empty; }}");
            //     else
            //         writer.WriteLine($"public override {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get => base.Message; }}");
            //     break;
            // case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
            //     writer.WriteLine($"[QueryParameter(\"{codeElement.SerializationName}\")]");
            //     goto default;
            // case CodePropertyKind.QueryParameters:
            //     defaultValue = $" = new {propertyType}();";
            //     goto default;
            default:
                // We don't have properties with getters/setters in AL, like in C#, so we need to create a function to get the property value

                // TODO: We will use properties for global vars and actual AL object properties and not as the equivalent from C#
                // if (codeElement.Documentation.DocumentationLabel == "Globals")
                // {
                //     if ((writer as ALWriter)?.GlobalsBlockStarted == false)
                //     {
                //         writer.StartBlock("var");
                //         (writer as ALWriter)?.SetGLobalsBlockStarted(true);
                //     }
                //     else
                //     {
                //         writer.IncreaseIndent();
                //     }
                //     writer.WriteLine($"{codeElement.Name}: {propertyType};");
                //     if ((writer as ALWriter)?.GlobalsBlockStarted == true)
                //         writer.DecreaseIndent();
                // }

                // writer.WriteLine($"procedure {codeElement.Name}() {(propertyType.EqualsIgnoreCase("void") ? ":" + propertyType : string.Empty)}");
                // writer.WriteLine("var");
                // writer.IncreaseIndent();
                // writer.WriteLine($"SubToken: JsonToken;");
                // writer.DecreaseIndent();
                // writer.StartBlock("begin");
                // writer.WriteLine($"if Token.SelectToken('{codeElement.Name}', SubToken) then");
                // writer.IncreaseIndent();
                // writer.WriteLine($"exit(SubToken.AsValue().As{propertyType}());");
                // writer.DecreaseIndent();
                // writer.CloseBlock("end;");
                break;
        }
    }
}
