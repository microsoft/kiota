using System.Linq;
using System.Collections.Generic;

namespace kiota.core {
    public class TypeScriptRefiner : ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "(input: object) => object"};
        public void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddRelativeImports(generatedCode);
        }
        private void AddRelativeImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                var additionalUsings = current
                                    .GetChildElements()
                                    .OfType<CodeProperty>()
                                    .Select(x =>x.Type)
                                    .Union(current.GetChildElements().OfType<CodeMethod>().Select(x => x.ReturnType))
                                    .Where(x => !defaultTypes.Contains(x.Name))
                                    .Select(x => new CodeUsing(currentClass){Name = x.Name, Declaration = x});
                if(additionalUsings.Any())
                    currentClass.AddUsing(additionalUsings.ToArray());
            }
            foreach(var childClass in current.GetChildElements())
                AddRelativeImports(childClass);
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
