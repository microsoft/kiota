using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CSharpConventionService>
    {
        public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
        {
            var parentClass = codeElement.Parent as CodeClass;
            var urlTemplateParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplateParameters);
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType, codeElement)} position] {{ get {{");
            writer.IncreaseIndent();
            conventions.AddParametersAssignment(writer, urlTemplateParametersProp, new (CodeTypeBase, string, string)[] {
                (codeElement.IndexType, codeElement.ParameterName, "position")
            });
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, "return ");
            writer.DecreaseIndent();
            writer.WriteLine("} }");
        }
    }
}
