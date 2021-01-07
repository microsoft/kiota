using System.Linq;

namespace kiota.core {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        public abstract void Refine(CodeNamespace generatedCode);

        internal void AddInnerClasses(CodeElement current) {
            if(current is CodeClass currentClass) {
                foreach(var parameter in current.GetChildElements().OfType<CodeMethod>().SelectMany(x =>x.Parameters).Where(x => x.Type.ActionOf))
                    currentClass.AddInnerClass(parameter.Type.TypeDefinition);
            }
            foreach(var childClass in current.GetChildElements())
                AddInnerClasses(childClass);
        }
    }
}
