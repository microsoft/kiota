using System.Linq;
using System.Text.RegularExpressions;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private Regex responseHandlerType = new Regex("<(.*),object>");
        public override void Refine(CodeNamespace generatedCode)
        {
            generatedCode.AddUsing(new CodeUsing() { Name = "System" });
            generatedCode.AddUsing(new CodeUsing() { Name = "System.Threading.Tasks" });
            generatedCode.AddUsing(new CodeUsing() { Name = "System.Net.Http" });
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode);
            MakeNativeResponseHandlers(generatedCode);
        }

        private void MakeNativeResponseHandlers(CodeNamespace generatedCode)
        {
            foreach (var codeElement in generatedCode.InnerChildElements)
            {
                switch (codeElement)
                {
                    case CodeClass c:
                        var responseHandlerProp = c.InnerChildElements.Where(e => e is CodeProperty && e.Name == "ResponseHandler")
                                                                        .Cast<CodeProperty>().FirstOrDefault();
                        if (responseHandlerProp != null)
                        {
                            responseHandlerProp.Type.Name = responseHandlerType.Replace(responseHandlerProp.Type.Name, "<HttpResponseMessage,Task<$1>>"); // TODO: We should probably generic types properly 
                        }
                        var defaultResponseHandler = c.InnerChildElements.Where(e => e is CodeMethod && e.Name == "DefaultResponseHandler")
                                                                      .Cast<CodeMethod>().FirstOrDefault();
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
