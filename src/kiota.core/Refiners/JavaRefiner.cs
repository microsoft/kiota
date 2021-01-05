using System.Linq;

namespace kiota.core {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(generatedCode);
            MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(generatedCode);
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
            
            foreach(var childElement in currentElement.GetChildElements())
                MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(childElement);
        }
        private CodeMethod GetMethodClone(CodeMethod currentMethod) {
            if(currentMethod.Parameters.Any(x => x.ParameterKind == CodeParameterKind.QueryParameter)) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.Parameters.RemoveAll(x => x.ParameterKind == CodeParameterKind.QueryParameter);
                return cloneMethod;
            }
            else return null;
        }
    }
}
