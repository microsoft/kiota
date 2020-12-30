using System.Linq;

namespace kiota.core {
    public class TypeScriptRefiner : ILanguageRefiner
    {
        private readonly string[] defaultTypes = new string[] {"string", "integer", "boolean", "array", "object"};
        public void Refine(CodeNamespace generatedCode)
        {
            AddRelativeImports(generatedCode);
        }
        private void AddRelativeImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                currentClass.AddUsing(current
                                    .GetChildElements()
                                    .OfType<CodeProperty>()
                                    .Select(x =>x.Type.Name)
                                    .Union(current.GetChildElements().OfType<CodeMethod>().Select(x => x.ReturnType.Name))
                                    .Distinct()
                                    .Except(defaultTypes)
                                    .Select(x => new CodeUsing{Name = x})
                                    .ToArray());
            }
            foreach(var childClass in current.GetChildElements())
                AddRelativeImports(childClass);
        }
    }
}
