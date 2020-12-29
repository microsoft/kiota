using System.Linq;

namespace kiota.core {
    public class CSharpRefiner : ILanguageRefiner
    {
        public void Refine(CodeNamespace generatedCode)
        {
            generatedCode.AddUsing(new CodeUsing() { Name = "System" });
            generatedCode.AddUsing(new CodeUsing() { Name = "System.Threading.Tasks" });
            AddInnerClasses(generatedCode);
        }
        private void AddInnerClasses(CodeElement current) {
            if(current is CodeClass currentClass) {
                foreach(var parameter in current.GetChildElements().OfType<CodeMethod>().SelectMany(x =>x.Parameters).Where(x => x.Type.ActionOf))
                    currentClass.AddInnerClass(parameter.Type.TypeDefinition);
            }
            foreach(var childClass in current.GetChildElements().OfType<CodeClass>())
                AddInnerClasses(childClass);
        }
    }
}
