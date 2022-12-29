using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CSharpConventionService>
{
    public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) {}
    public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
    {
        var parentClass = codeElement.Parent as CodeClass;
        var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        writer.StartBlock($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType, codeElement)} position] {{ get {{");
        conventions.AddParametersAssignment(writer, pathParametersProp.Type, pathParametersProp.Name.ToFirstCharacterUpperCase(), (codeElement.IndexType, codeElement.SerializationName, "position"));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, "return ");
        writer.CloseBlock("} }");
    }
}
