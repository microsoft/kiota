using System.Linq;
using System.Text.RegularExpressions;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private static readonly Regex responseHandlerType = new Regex("<(.*),object>");
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode);
            MakeNativeResponseHandlers(generatedCode);
        }
        private static readonly string[] defaultNamespacesForClasses = new string[] {"System", "System.Threading.Tasks", "System.Net.Http"};
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass)
                currentClass.AddUsing(defaultNamespacesForClasses.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
            foreach(var childElement in current.GetChildElements())
                AddDefaultImports(childElement);
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
