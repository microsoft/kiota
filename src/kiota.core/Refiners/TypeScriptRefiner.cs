using System.Linq;
using System.Collections.Generic;

namespace kiota.core {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "(input: object) => object"};
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        }
        private void PatchResponseHandlerType(CodeElement current) {
            var properties = current.GetChildElements()
                .OfType<CodeProperty>();
            properties
                .Where(x => x.PropertyKind == CodePropertyKind.ResponseHandler)
                .ToList()
                .ForEach(x => x.Type.Name = "(input: object) => object");
            current.GetChildElements()
                .Except(properties)
                .ToList()
                .ForEach(x => PatchResponseHandlerType(x));
        }
    }
}
