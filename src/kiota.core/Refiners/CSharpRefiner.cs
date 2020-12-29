using System.Linq;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            generatedCode.AddUsing(new CodeUsing() { Name = "System" });
            generatedCode.AddUsing(new CodeUsing() { Name = "System.Threading.Tasks" });
            AddInnerClasses(generatedCode);
        }
    }
}
