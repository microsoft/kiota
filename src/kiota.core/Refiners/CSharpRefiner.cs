using System.Linq;
using System.Text.RegularExpressions;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private static readonly Regex responseHandlerType = new Regex("<(.*),object>");
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode);
            MakeNativeResponseHandlers(generatedCode);
        }
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                currentClass.AddUsing(new CodeUsing(currentClass) { Name = "System" });
                currentClass.AddUsing(new CodeUsing(currentClass) { Name = "System.Threading.Tasks" });
                currentClass.AddUsing(new CodeUsing(currentClass) { Name = "System.Net.Http" });
            }
            foreach(var childClass in current.GetChildElements())
                AddDefaultImports(childClass);
        }

        private void MakeNativeResponseHandlers(CodeNamespace generatedCode)
        {
            foreach (var codeElement in generatedCode.InnerChildElements)
            {
                switch (codeElement)
                {
                    case CodeClass c:
                        var responseHandlerProp = c.InnerChildElements.OfType<CodeProperty>().Where(e => e.PropertyKind == CodePropertyKind.ResponseHandler)
                                                                        .FirstOrDefault();
                        if (responseHandlerProp != null)
                        {
                            responseHandlerProp.Type.Name = responseHandlerType.Replace(responseHandlerProp.Type.Name, "<HttpResponseMessage,Task<$1>>"); // TODO: We should probably generic types properly 
                        }
                        var defaultResponseHandler = c.InnerChildElements.OfType<CodeMethod>()
                                                                            .Where(m=> m.MethodKind == CodeMethodKind.ResponseHandler)
                                                                            .FirstOrDefault();
                        if (defaultResponseHandler != null)
                        {
                            defaultResponseHandler.Parameters.FirstOrDefault().Type.Name = "HttpResponseMessage";
                        }
                        break;
                    case CodeNamespace n:
                        MakeNativeResponseHandlers(n);
                        break;
                }
            }
        }
        private void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod)
                currentMethod.Name += "Async";
            foreach(var childElement in currentElement.GetChildElements())
                AddAsyncSuffix(childElement);
        }
    }
}
