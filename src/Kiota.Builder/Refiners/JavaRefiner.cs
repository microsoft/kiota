using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object"};
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(generatedCode);
            MakeQueryStringParametersNonOptionalAndInsertOverrideMethod(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode);
            AddRequireNonNullImports(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode);
            CorrectCoreType(generatedCode);
            PatchHeaderParametersType(generatedCode);
            AddListImport(generatedCode);
        }
        private void AddListImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                (currentClass.InnerChildElements.OfType<CodeProperty>().Any(x => x.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                currentClass.InnerChildElements.OfType<CodeMethod>().Any(x => x.ReturnType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                currentClass.InnerChildElements.OfType<CodeMethod>().Any(x => x.Parameters.Any(y => y.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex))
                )) {
                    var nUsing = new CodeUsing(currentClass) {
                        Name = "java.util"
                    };
                    nUsing.Declaration = new CodeType(nUsing) { Name = "List" };
                    currentClass.AddUsing(nUsing);
                }
            CrawlTree(currentElement, AddListImport);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "com.microsoft.kiota"),
            new ("HttpMethod", "com.microsoft.kiota"),
            new ("RequestInfo", "com.microsoft.kiota"),
            new ("ResponseHandler", "com.microsoft.kiota"),
            new ("QueryParametersBase", "com.microsoft.kiota"),
            new ("Map", "java.util"),
            new ("URI", "java.net"),
            new ("URISyntaxException", "java.net"),
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.RequestBuilder) {
                currentClass.AddUsing(defaultNamespacesForRequestBuilders
                                        .Select(x => {
                                                        var nUsing = new CodeUsing(currentClass) { 
                                                            Name = x.Item2,
                                                        };
                                                        nUsing.Declaration = new CodeType(nUsing) { Name = x.Item1, IsExternal = true };
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
        private void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Optional)) {
                var parentClass = currentMethod.Parent as CodeClass;
                var newUsing = new CodeUsing(parentClass) {
                    Name = "java.util",
                };
                newUsing.Declaration = new CodeType(newUsing) {
                    Name = "Objects",
                    IsExternal = true,
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
                    .Where(x => x.ParameterKind == CodeParameterKind.QueryParameter || x.ParameterKind == CodeParameterKind.Headers || x.ParameterKind == CodeParameterKind.ResponseHandler)
                    .ToList()
                    .ForEach(x => x.Optional = false);
                var methodsToAdd = codeMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                    .Union(codeMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers)))
                                    .Union(codeMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.ResponseHandler)))
                                    .Where(x => x != null);
                if(methodsToAdd.Any())
                    currentClass.AddMethod(methodsToAdd.ToArray());
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
    }
}
