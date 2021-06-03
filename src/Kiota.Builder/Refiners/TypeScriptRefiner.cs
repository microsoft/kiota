using System.Linq;
using System.Collections.Generic;
using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, "ById");
            CorrectCoreType(generatedCode);
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "ReadableStream", "web-streams-polyfill/es2018", true);
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
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
            new ("SerializationWriterFactory", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("SerializationWriter", "@microsoft/kiota-abstractions"),
            new ("ParseNode", "@microsoft/kiota-abstractions"),
            new ("Parsable", "@microsoft/kiota-abstractions"),
        };
        private static void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty) {
                if ("IHttpCore".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.Name.Equals("serializerFactory", StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = "SerializationWriterFactory";
                else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = $"Date";
                else if(currentProperty.PropertyKind == CodePropertyKind.AdditionalData) {
                    currentProperty.Type.Name = "Map<string, unknown>";
                    currentProperty.DefaultValue = "new Map<string, unknown>()";
                }
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.MethodKind == CodeMethodKind.RequestExecutor)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.MethodKind == CodeMethodKind.Serializer)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
                else if(currentMethod.MethodKind == CodeMethodKind.Deserializer)
                    currentMethod.ReturnType.Name = $"Map<string, (item: {currentMethod.Parent.Name.ToFirstCharacterUpperCase()}, node: ParseNode) => void>";
            }
            CrawlTree(currentElement, CorrectCoreType);
        }
        private static void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.OrdinalIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
