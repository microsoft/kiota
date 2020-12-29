using System.Linq;

namespace kiota.core {
    public class TypeScriptRefiner : ILanguageRefiner
    {
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
                                    .Distinct()
                                    .Select(x => new CodeUsing{Name = x})
                                    .ToArray());
            }
            foreach(var childClass in current.GetChildElements().OfType<CodeClass>())
                AddRelativeImports(childClass);
        }
    }
}
