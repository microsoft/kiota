using System.Linq;
using System.Collections.Generic;

namespace kiota.core {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "(input: object) => object"};
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, "ById");
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        }
        private void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeProperty currentProperty && currentProperty.PropertyKind == CodePropertyKind.ResponseHandler) {
                currentProperty.Type.Name = "(input: object) => Promise<object>";
                currentProperty.DefaultValue = "this.defaultResponseHandler";
            }
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
