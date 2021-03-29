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
            AndInsertOverrideMethodForRequestExecutorsAndBuilders(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode);
            AddRequireNonNullImports(generatedCode);
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode);
            CorrectCoreType(generatedCode);
            PatchHeaderParametersType(generatedCode);
            AddListImport(generatedCode);
            AddParsableInheritanceForModelClasses(generatedCode);
            ConvertDeserializerPropsToMethods(generatedCode, "get");
            ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true);
        }
        private void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private void AddListImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                (currentClass.InnerChildElements.OfType<CodeProperty>().Any(x => x.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                currentClass.InnerChildElements.OfType<CodeMethod>().Any(x => x.ReturnType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                currentClass.InnerChildElements.OfType<CodeMethod>().Any(x => x.Parameters.Any(y => y.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex))
                )) {
                    var nUsing = new CodeUsing(currentClass) {
                        Name = "List"
                    };
                    nUsing.Declaration = new CodeType(nUsing) { Name = "java.util", IsExternal = true };
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
            new ("InputStream", "java.io"),
            new ("Function", "java.util.function"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "com.microsoft.kiota.serialization"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "com.microsoft.kiota.serialization"),
            new ("Parsable", "com.microsoft.kiota.serialization"),
            new ("BiConsumer", "java.util.function"),
            new ("Map", "java.util"),
            new ("HashMap", "java.util"),
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                if(currentClass.ClassKind == CodeClassKind.Model)
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForModels)
                                            .Select(x => {
                                                            var nUsing = new CodeUsing(currentClass) { 
                                                                Name = x.Item1,
                                                            };
                                                            nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                                            return nUsing;
                                                        }).ToArray());
                if(currentClass.ClassKind == CodeClassKind.RequestBuilder)
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForRequestBuilders)
                                            .Select(x => {
                                                            var nUsing = new CodeUsing(currentClass) { 
                                                                Name = x.Item1,
                                                            };
                                                            nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                                            return nUsing;
                                                        }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty) {
                if(currentProperty.Type.Name?.Equals("IHttpCore", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.Name.Equals("serializerFactory", StringComparison.InvariantCultureIgnoreCase))
                    currentProperty.Type.Name = "Function<String, SerializationWriter>";
                else if(currentProperty.Name.Equals("deserializeFields", StringComparison.InvariantCultureIgnoreCase))
                    currentProperty.Type.Name = $"Map<String, BiConsumer<T, ParseNode>>";
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.MethodKind == CodeMethodKind.RequestExecutor)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.MethodKind == CodeMethodKind.Serializer)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
            }
            CrawlTree(currentElement, CorrectCoreType);
        }
        private void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Optional)) {
                var parentClass = currentMethod.Parent as CodeClass;
                var newUsing = new CodeUsing(parentClass) {
                    Name = "Objects",
                };
                newUsing.Declaration = new CodeType(newUsing) {
                    Name = "java.util",
                    IsExternal = true,
                };
                parentClass?.AddUsing(newUsing);
            }
            CrawlTree(currentElement, AddRequireNonNullImports);
        }
        private void AndInsertOverrideMethodForRequestExecutorsAndBuilders(CodeElement currentElement) {
            var codeMethods = currentElement
                                    .GetChildElements()
                                    .OfType<CodeMethod>();
            if(currentElement is CodeClass currentClass && codeMethods.Any()) {
                var originalExecutorMethods = codeMethods.Where(x => x.MethodKind == CodeMethodKind.RequestExecutor);
                var executorMethodsToAdd = originalExecutorMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                    .Union(originalExecutorMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers)))
                                    .Union(originalExecutorMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.ResponseHandler)))
                                    .Where(x => x != null);
                var originalGeneratorMethods = codeMethods.Where(x => x.MethodKind == CodeMethodKind.RequestGenerator);
                var generatorMethodsToAdd = originalGeneratorMethods
                                    .Where(x => x.MethodKind == CodeMethodKind.RequestGenerator)
                                    .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                    .Union(originalGeneratorMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers)))
                                    .Where(x => x != null);
                if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                    currentClass.AddMethod(executorMethodsToAdd.Union(generatorMethodsToAdd).ToArray());
            }
            
            CrawlTree(currentElement, AndInsertOverrideMethodForRequestExecutorsAndBuilders);
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
