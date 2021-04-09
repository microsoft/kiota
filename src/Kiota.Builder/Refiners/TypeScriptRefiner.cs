using System.Linq;
using System.Collections.Generic;
using System;

namespace Kiota.Builder {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, "ById");
            CorrectCoreType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "@microsoft/kiota-abstractions"),
            new ("HttpMethod", "@microsoft/kiota-abstractions"),
            new ("RequestInfo", "@microsoft/kiota-abstractions"),
            new ("ResponseHandler", "@microsoft/kiota-abstractions"),
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.RequestBuilder) {
                currentClass.AddUsing(defaultNamespacesForRequestBuilders
                                        .Select(x => {
                                            var nUsing = new CodeUsing(currentClass) { Name = x.Item1 };
                                            nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                            return nUsing;
                                            }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty && (currentProperty.Type.Name?.Equals("IHttpCore", StringComparison.InvariantCultureIgnoreCase) ?? false))
                currentProperty.Type.Name = "HttpCore";
            if (currentElement is CodeMethod currentMethod)
                currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
            CrawlTree(currentElement, CorrectCoreType);
        }
        private void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.InvariantCultureIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
