using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CSharpConventionService>
    {
        public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) {}
        private const string TempDictionaryVarName = "urlTplParams";
        public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
        {
            var parentClass = codeElement.Parent as CodeClass;
            var urlTemplateParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplateParameters);
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType, codeElement)} position] {{ get {{");
            writer.IncreaseIndent();
            var stringSuffix = codeElement.IndexType.Name.Equals("string", StringComparison.OrdinalIgnoreCase) ? string.Empty : ".ToString()";
            writer.WriteLines($"var {TempDictionaryVarName} = new {urlTemplateParametersProp.Type.Name}({urlTemplateParametersProp.Name.ToFirstCharacterUpperCase()});",
                                $"{TempDictionaryVarName}.Add(\"{codeElement.ParameterName}\", position{stringSuffix});");
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, TempDictionaryVarName, "return ");
            writer.DecreaseIndent();
            writer.WriteLine("} }");
        }
    }
}
