using System.Collections.Generic;
using System.Linq;

namespace kiota.core {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "java.util.function.Function<Object,Object>"};
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddInnerClasses(generatedCode);
            MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
        }
        private void MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(CodeElement currentElement) {
            var codeMethods = currentElement
                                    .GetChildElements()
                                    .OfType<CodeMethod>();
            if(currentElement is CodeClass currentClass && codeMethods.Any()) {
                codeMethods
                    .SelectMany(x => x.Parameters)
                    .Where(x => x.ParameterKind == CodeParameterKind.QueryParameter)
                    .ToList()
                    .ForEach(x => x.Optional = false);
                currentClass.AddMethod(codeMethods
                                    .Select(x => GetMethodClone(x))
                                    .Where(x => x != null)
                                    .ToArray());
            }
            
            CrawlTree(currentElement, MakeQueryStringParametersNonOptionalAndInsertOverrideMethod);
        }
        private CodeMethod GetMethodClone(CodeMethod currentMethod) {
            if(currentMethod.Parameters.Any(x => x.ParameterKind == CodeParameterKind.QueryParameter)) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.Parameters.RemoveAll(x => x.ParameterKind == CodeParameterKind.QueryParameter);
                return cloneMethod;
            }
            else return null;
        }
        private void PatchResponseHandlerType(CodeElement current) {
            var properties = current.GetChildElements()
                .OfType<CodeProperty>();
            properties
                .Where(x => x.PropertyKind == CodePropertyKind.ResponseHandler)
                .ToList()
                .ForEach(x => x.Type.Name = "java.util.function.Function<Object,Object>");
            current.GetChildElements()
                .Except(properties)
                .ToList()
                .ForEach(x => PatchResponseHandlerType(x));
        }
    }
}
