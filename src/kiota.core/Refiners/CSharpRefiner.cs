using System.Linq;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            generatedCode.AddUsing(new CodeUsing() { Name = "System" });
            generatedCode.AddUsing(new CodeUsing() { Name = "System.Threading.Tasks" });
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode);
        }
        private void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod)
                currentMethod.Name += "Async";
            foreach(var childElement in currentElement.GetChildElements())
                AddAsyncSuffix(childElement);
        }
    }
}
