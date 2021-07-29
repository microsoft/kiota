using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Refiners {
    public class GoRefiner : CommonLanguageRefiner
    {
        public GoRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                generatedCode);
            MoveModelsInDedicatedNamespace(
                generatedCode
            );
            AddPropertiesAndMethodTypesImports(
                generatedCode,
                true,
                false,
                true);
            AddGetterAndSetterMethods(
                generatedCode, 
                new () { 
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.Custom,
                    CodePropertyKind.BackingStore }, 
                _configuration.UsesBackingStore,
                false);
            AddDefaultImports(
                generatedCode,
                defaultNamespaces,
                defaultNamespacesForModels,
                defaultNamespacesForRequestBuilders,
                defaultSymbolsForApiClient);
            CorrectCoreType(
                generatedCode);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "github.com/microsoft/kiota/abstractions/go"),
            new ("HttpMethod", "github.com/microsoft/kiota/abstractions/go"),
            new ("RequestInfo", "github.com/microsoft/kiota/abstractions/go"),
            new ("ResponseHandler", "github.com/microsoft/kiota/abstractions/go"),
            // new ("QueryParametersBase", "github.com/microsoft/kiota/abstractions/go"),
            // new ("Map", "java.util"),
            // new ("URI", "java.net"),
            // new ("URISyntaxException", "java.net"),
            // new ("InputStream", "java.io"),
            // new ("Function", "java.util.function"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "github.com/microsoft/kiota/abstractions/go/serialization"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "github.com/microsoft/kiota/abstractions/go/serialization"),
            new ("Parsable", "github.com/microsoft/kiota/abstractions/go/serialization"),
            // new ("BiConsumer", "java.util.function"),
            // new ("Map", "java.util"),
            // new ("HashMap", "java.util"),
        };
        private static readonly Tuple<string, string>[] defaultSymbolsForApiClient = new Tuple<string, string>[] { 
            // new ("ApiClientBuilder", "github.com/microsoft/kiota/abstractions/go"),
            // new ("SerializationWriterFactoryRegistry", "github.com/microsoft/kiota/abstractions/go/serialization"),
            // new ("ParseNodeFactoryRegistry", "github.com/microsoft/kiota/abstractions/go/serialization"),
        };
        private static void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty && currentProperty.Type != null) {
                if(currentProperty.IsOfKind(CodePropertyKind.HttpCore))
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                    currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
                else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase)) {
                    currentProperty.Type.Name = $"Time";
                    var nUsing = new CodeUsing(currentProperty.Parent) {
                        Name = "Time",
                    };
                    nUsing.Declaration = new CodeType(nUsing) {
                        Name = "time",
                        IsExternal = true,
                    };
                    (currentProperty.Parent as CodeClass).AddUsing(nUsing);
                } else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                    currentProperty.Type.Name = "map[string]interface{}";
                    currentProperty.DefaultValue = "new HashMap<>()";
                }
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
                else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
                    currentMethod.ReturnType.Name = $"Map<String, BiConsumer<T, ParseNode>>";
                    currentMethod.Name = "getFieldDeserializers";
                }
                else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.HttpCore))
                        .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                        .ToList()
                        .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            }
            CrawlTree(currentElement, CorrectCoreType);
        }
        private static void MoveModelsInDedicatedNamespace(CodeElement currentElement, CodeNamespace targetNamespace = default) {
            if(targetNamespace == default &&
                currentElement is CodeNamespace currentNS &&
                !string.IsNullOrEmpty(currentNS.Name) &&
                currentNS.Name.Contains('/'))
                    targetNamespace = currentNS.AddNamespace($"{currentNS.Name}.models");
            if(currentElement.Parent is CodeNamespace parentNS) {
                if(currentElement is CodeClass currentClass &&
                    currentClass.IsOfKind(CodeClassKind.Model) &&
                    !currentClass.Name.EndsWith("response", StringComparison.OrdinalIgnoreCase)) {
                        targetNamespace.AddClass(currentClass);
                        parentNS.RemoveChildElement(currentClass);
                    }
                if(currentElement is CodeEnum currentEnum) {
                    targetNamespace.AddEnum(currentEnum);
                    parentNS.RemoveChildElement(currentEnum);
                }
            }
            CrawlTree(currentElement, (x) => MoveModelsInDedicatedNamespace(x, targetNamespace));
        }
    }
}
