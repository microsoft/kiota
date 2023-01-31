using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CSharpConventionService>
{
    public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
    {
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        writer.StartBlock($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType, codeElement)} position] {{ get {{");
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp)
            conventions.AddParametersAssignment(writer, pathParametersProp.Type, pathParametersProp.Name.ToFirstCharacterUpperCase(), (codeElement.IndexType, codeElement.SerializationName, "position", false));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, "return ");
        writer.CloseBlock("} }");
    }
}
