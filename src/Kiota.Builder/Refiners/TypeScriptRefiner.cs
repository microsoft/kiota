using System.Linq;
using System.Collections.Generic;
using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "(input: ReadableStream) => object"};
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, "ById");
            CorrectCoreType(generatedCode);
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableInheritanceForModelClasses(generatedCode);
        }
        private void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"Parsable<{currentClass.Name.ToFirstCharacterUpperCase()}>",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "@microsoft/kiota-abstractions"),
            new ("HttpMethod", "@microsoft/kiota-abstractions"),
            new ("RequestInfo", "@microsoft/kiota-abstractions"),
            new ("ResponseHandler", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "@microsoft/kiota-abstractions"),
            new ("Parsable", "@microsoft/kiota-abstractions"),
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                if(currentClass.ClassKind == CodeClassKind.Model)
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForModels)
                                            .Select(x => {
                                                var nUsing = new CodeUsing(currentClass) { Name = x.Item1 };
                                                nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                                return nUsing;
                                                }).ToArray());
                if(currentClass.ClassKind == CodeClassKind.RequestBuilder)
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForRequestBuilders)
                                            .Select(x => {
                                                var nUsing = new CodeUsing(currentClass) { Name = x.Item1 };
                                                nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                                return nUsing;
                                                }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty) {
                if (currentProperty.Type.Name?.Equals("IHttpCore", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.Name.Equals("serializerFactory", StringComparison.InvariantCultureIgnoreCase))
                    currentProperty.Type.Name = "((mediaType: string) => SerializationWriter)";
                else if(currentProperty.Name.Equals("deserializeFields", StringComparison.InvariantCultureIgnoreCase))
                    currentProperty.Type.Name = $"Map<string, (item: {currentProperty.Parent.Name.ToFirstCharacterUpperCase()}, node: ParseNode) => void>";
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.MethodKind == CodeMethodKind.RequestExecutor)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.MethodKind == CodeMethodKind.Serializer)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
            }
            CrawlTree(currentElement, CorrectCoreType);
        }
        private void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.InvariantCultureIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
