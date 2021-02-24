using System;
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
            ReplaceIndexersByMethodsWithParameter(generatedCode);
            AddRequireNonNullImports(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode);
            CorrectCoreType(generatedCode);
            PatchHeaderParametersType(generatedCode);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "com.microsoft.kiota"),
            new ("HttpMethod", "com.microsoft.kiota"),
            new ("RequestInfo", "com.microsoft.kiota"),
            new ("QueryParametersBase", "com.microsoft.kiota"),
            new ("Map", "java.util"),
            new ("URI", "java.net"),
            new ("URISyntaxException", "java.net"),
            new ("InputStream", "java.io"),
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.RequestBuilder) {
                currentClass.AddUsing(defaultNamespacesForRequestBuilders
                                        .Select(x => {
                                                        var nUsing = new CodeUsing(currentClass) { 
                                                            Name = x.Item2,
                                                        };
                                                        nUsing.Declaration = new CodeType(nUsing) { Name = x.Item1 };
                                                        return nUsing;
                                                    }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty && currentProperty.Type.Name.Equals("IHttpCore", StringComparison.InvariantCultureIgnoreCase))
                currentProperty.Type.Name = "HttpCore";
            CrawlTree(currentElement, CorrectCoreType);
        }
        private void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Optional)) {
                var parentClass = currentMethod.Parent as CodeClass;
                var newUsing = new CodeUsing(parentClass) {
                    Name = "java.util",
                };
                newUsing.Declaration = new CodeType(newUsing) {
                    Name = "Objects"
                };
                parentClass?.AddUsing(newUsing);
            }
            CrawlTree(currentElement, AddRequireNonNullImports);
        }
        private void MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(CodeElement currentElement) {
            var codeMethods = currentElement
                                    .GetChildElements()
                                    .OfType<CodeMethod>();
            if(currentElement is CodeClass currentClass && codeMethods.Any()) {
                codeMethods
                    .SelectMany(x => x.Parameters)
                    .Where(x => x.ParameterKind == CodeParameterKind.QueryParameter || x.ParameterKind == CodeParameterKind.Headers)
                    .ToList()
                    .ForEach(x => x.Optional = false);
                currentClass.AddMethod(codeMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                    .Where(x => x != null)
                                    .ToArray());
                currentClass.AddMethod(codeMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers))
                                    .Where(x => x != null)
                                    .ToArray());
            }
            
            CrawlTree(currentElement, MakeQueryStringParametersNonOptionalAndInsertOverrideMethod);
        }
        private void PatchHeaderParametersType(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => x.ParameterKind == CodeParameterKind.Headers))
                currentMethod.Parameters.Where(x => x.ParameterKind == CodeParameterKind.Headers)
                                        .ToList()
                                        .ForEach(x => x.Type.Name = "Map<String, String>");
            CrawlTree(currentElement, PatchHeaderParametersType);
        }
        private CodeMethod GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude) {
            if(currentMethod.Parameters.Any(x => parameterTypesToExclude.Contains(x.ParameterKind))) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.Parameters.RemoveAll(x => parameterTypesToExclude.Contains(x.ParameterKind));
                return cloneMethod;
            }
            else return null;
        }
        private void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeProperty currentProperty && currentProperty.PropertyKind == CodePropertyKind.ResponseHandler) {
                currentProperty.Type.Name = "java.util.function.Function<InputStream,java.util.concurrent.CompletableFuture<Object>>";
                currentProperty.DefaultValue = "x -> defaultResponseHandler(x)";
            }
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
