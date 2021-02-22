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
        }
        private void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Type.IsNullable)) {
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
        private const string pathSegmentPropertyName = "pathSegment";
        private void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement) {
            if(currentElement is CodeIndexer currentIndexer) {
                var currentParentClass = currentElement.Parent as CodeClass;
                currentParentClass.InnerChildElements.Remove(currentElement);
                var pathSegment = currentParentClass
                                    .GetChildElements()
                                    .OfType<CodeProperty>()
                                    .FirstOrDefault(x => x.Name.Equals(pathSegmentPropertyName, StringComparison.InvariantCultureIgnoreCase))
                                    ?.DefaultValue;
                if(!string.IsNullOrEmpty(pathSegment))
                    AddIndexerMethod(currentElement.GetImmediateParentOfType<CodeNamespace>().GetRootNamespace(), currentParentClass, currentIndexer.ReturnType.TypeDefinition, pathSegment.Trim('\"').TrimStart('/'));
            }
            CrawlTree(currentElement, ReplaceIndexersByMethodsWithParameter);
        }
        private void AddIndexerMethod(CodeElement currentElement, CodeClass targetClass, CodeClass indexerClass, string pathSegment) {
            if(currentElement is CodeProperty currentProperty && currentProperty.Type.TypeDefinition == targetClass) {
                var parentClass = currentElement.Parent as CodeClass;
                var method = new CodeMethod(parentClass) {
                    IsAsync = false,
                    IsStatic = false,
                    Access = AccessModifier.Public,
                    MethodKind = CodeMethodKind.IndexerBackwardCompatibility,
                    Name = pathSegment,
                };
                method.ReturnType = new CodeType(method) {
                    IsNullable = false,
                    TypeDefinition = indexerClass,
                    Name = indexerClass.Name,
                };
                method.GenerationProperties.Add(pathSegmentPropertyName, pathSegment);
                var parameter = new CodeParameter(method) {
                    Name = "id",
                    Optional = false,
                    ParameterKind = CodeParameterKind.Custom
                };
                parameter.Type = new CodeType(parameter) {
                    Name = "String",
                    IsNullable = false,
                };
                method.Parameters.Add(parameter);
                parentClass.AddMethod(method);
            }
            CrawlTree(currentElement, c => AddIndexerMethod(c, targetClass, indexerClass, pathSegment));
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
            if(current is CodeProperty currentProperty && currentProperty.PropertyKind == CodePropertyKind.ResponseHandler) {
                currentProperty.Type.Name = "java.util.function.Function<Object,java.util.concurrent.CompletableFuture<Object>>";
                currentProperty.DefaultValue = "x -> defaultResponseHandler(x)";
            }
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
