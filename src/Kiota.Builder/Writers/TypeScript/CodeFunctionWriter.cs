

using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter : BaseElementWriter<CodeFunction, TypeScriptConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeFunctionWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService){
        _codeUsingWriter = new (clientNamespaceName);
    }

    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if(codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        if(writer == null) throw new ArgumentNullException(nameof(writer));
        if(!(codeElement.Parent is CodeNamespace)) throw new InvalidOperationException("the parent of a method should be a namespace");

        var returnType = conventions.GetTypeString(codeElement.OriginalLocalMethod.ReturnType, codeElement);
        _codeUsingWriter.WriteCodeElement(codeElement.StartBlock.Usings, codeElement.GetImmediateParentOfType<CodeNamespace>(), writer);
        CodeMethodWriter.WriteMethodPrototypeInternal(codeElement.OriginalLocalMethod, writer, returnType, false, conventions, true);
        writer.IncreaseIndent();
        CodeMethodWriter.WriteDefensiveStatements(codeElement.OriginalLocalMethod, writer);
        WriteFactoryMethodBody(codeElement, returnType, writer);
        writer.CloseBlock();
    }

    private static void WriteFactoryMethodBody(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.OriginalLocalMethod.Parameters.OfKind(CodeParameterKind.ParseNode);
        if(codeElement.OriginalLocalMethod.ShouldWriteDiscriminatorSwitch && parseNodeParameter != null) {
            writer.WriteLines($"const mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.getChildNode(\"{codeElement.OriginalLocalMethod.DiscriminatorPropertyName}\");",
                                $"if (mappingValueNode) {{");
            writer.IncreaseIndent();
            writer.WriteLines($"const mappingValue = mappingValueNode.getStringValue();",
                            "if (mappingValue) {");
            writer.IncreaseIndent();

            writer.WriteLine($"switch (mappingValue) {{");
            writer.IncreaseIndent();
            foreach(var mappedType in codeElement.OriginalLocalMethod.DiscriminatorMappings) {
                writer.WriteLine($"case \"{mappedType.Key}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"return new {mappedType.Value.Name.ToFirstCharacterUpperCase()}();");
                writer.DecreaseIndent();
            }
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }

        writer.WriteLine($"return new {returnType.ToFirstCharacterUpperCase()}();");
    }
}
