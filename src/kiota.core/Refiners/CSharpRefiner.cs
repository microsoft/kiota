using System.Linq;
using System.Text.RegularExpressions;

namespace kiota.core {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private static readonly Regex responseHandlerType = new Regex("<(.*),object>");
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode);
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
        private void AddPropertiesAndMethodTypesImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
                var properties = currentClass
                                    .InnerChildElements
                                    .OfType<CodeProperty>()
                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)
                                    .Select(x => x.Type?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())
                                    .Where(x => x != currentClassNamespace && x != null)
                                    .Distinct();
                var methods = currentClass
                                    .InnerChildElements
                                    .OfType<CodeMethod>()
                                    .Where(x => x.MethodKind == CodeMethodKind.Custom)
                                    .SelectMany(x => x.Parameters)
                                    .Where(x => x.ParameterKind == CodeParameterKind.Custom)
                                    .Select(x => x.Type?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())
                                    .Where(x => x != currentClassNamespace && x != null)
                                    .Distinct();
                var usingsToAdd = properties
                                        .Union(methods)
                                        .Distinct()
                                        .Select(x => new CodeUsing(currentClass) { Name = x.Name })
                                        .ToArray();
                if(usingsToAdd.Any())
                    currentClass.AddUsing(usingsToAdd);
            }
            foreach(var childElement in current.GetChildElements())
                AddPropertiesAndMethodTypesImports(childElement);
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
