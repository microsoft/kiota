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
            CapitalizeNamespacesFirstLetters(generatedCode);
        }
        private static readonly string[] defaultNamespacesForClasses = new string[] {"System", "System.Threading.Tasks", "System.IO"};
        private static readonly string[] defaultNamespacesForRequestBuilders = new string[] { "System.Collections.Generic", "Kiota.Abstractions"};
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                currentClass.AddUsing(defaultNamespacesForClasses.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
                if(currentClass.ClassKind == CodeClassKind.RequestBuilder)
                    currentClass.AddUsing(defaultNamespacesForRequestBuilders.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CapitalizeNamespacesFirstLetters(CodeElement current) {
            if(current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
        private void MakeNativeResponseHandlers(CodeElement currentElement)
        {
            if(currentElement is CodeClass currentClass) {
                var responseHandlerProp = currentClass.InnerChildElements.OfType<CodeProperty>().Where(e => e.PropertyKind == CodePropertyKind.ResponseHandler)
                                                                .FirstOrDefault();
                if (responseHandlerProp != null)
                {
                    responseHandlerProp.Type.Name = responseHandlerType.Replace(responseHandlerProp.Type.Name, "<Stream,Task<$1>>"); // TODO: We should probably generic types properly 
                }
                var defaultResponseHandler = currentClass.InnerChildElements.OfType<CodeMethod>()
                                                                    .Where(m=> m.MethodKind == CodeMethodKind.ResponseHandler)
                                                                    .FirstOrDefault();
                if (defaultResponseHandler != null)
                {
                    defaultResponseHandler.Parameters.FirstOrDefault().Type.Name = "Stream";
                }
            }
            CrawlTree(currentElement, MakeNativeResponseHandlers);
        }
        private void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod)
                currentMethod.Name += "Async";
            CrawlTree(currentElement, AddAsyncSuffix);
        }
    }
}
