using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Refiners {
    public class GoRefiner : CommonLanguageRefiner
    {
        public GoRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(
                generatedCode,
                true);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                generatedCode,
                "ById");
            ReplaceRequestBuilderPropertiesByMethods(
                generatedCode
            );
            MoveModelsInDedicatedNamespace(
                generatedCode
            );
            AddPropertiesAndMethodTypesImports(
                generatedCode,
                true,
                false,
                true);
            AddDefaultImports(
                generatedCode,
                defaultNamespaces,
                defaultNamespacesForModels,
                defaultNamespacesForRequestBuilders);
            CorrectCoreType(
                generatedCode,
                CorrectMethodType,
                CorrectPropertyType);
            PatchHeaderParametersType(
                generatedCode,
                "map[string]string");
            AddGetterAndSetterMethods(
                generatedCode, 
                new () { 
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.Custom,
                    CodePropertyKind.BackingStore }, 
                _configuration.UsesBackingStore,
                false);
            AddConstructorsForDefaultValues(
                generatedCode,
                true);
            MakeModelPropertiesNullable(
                generatedCode);
            AddErrorImportForEnums(
                generatedCode);
            ReplaceDefaultSerializationModules(
                generatedCode,
                "github.com/microsoft/kiota/serialization/go/json.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(
                generatedCode,
                "github.com/microsoft/kiota/serialization/go/json.JsonParseNodeFactory");
            AddSerializationModulesImport(
                generatedCode,
                new string[] {"github.com/microsoft/kiota/abstractions/go/serialization.SerializationWriterFactory", "github.com/microsoft/kiota/abstractions/go.RegisterDefaultSerializer"},
                new string[] {"github.com/microsoft/kiota/abstractions/go/serialization.ParseNodeFactory", "github.com/microsoft/kiota/abstractions/go.RegisterDefaultDeserializer"});
        }
        private static void ReplaceRequestBuilderPropertiesByMethods(CodeElement currentElement) {
            if(currentElement is CodeProperty currentProperty &&
                currentProperty.IsOfKind(CodePropertyKind.RequestBuilder) &&
                currentElement.Parent is CodeClass parentClass) {
                    parentClass.RemoveChildElement(currentProperty);
                    currentProperty.Type.IsNullable = false;
                    parentClass.AddMethod(new CodeMethod(parentClass) {
                        Name = currentProperty.Name,
                        ReturnType = currentProperty.Type,
                        Access = AccessModifier.Public,
                        Description = currentProperty.Description,
                        IsAsync = false,
                        MethodKind = CodeMethodKind.RequestBuilderBackwardCompatibility,
                    });
                }
            CrawlTree(currentElement, ReplaceRequestBuilderPropertiesByMethods);
        }
        private static void AddErrorImportForEnums(CodeElement currentElement) {
            if(currentElement is CodeEnum currentEnum) {
                currentEnum.Usings.Add(new CodeUsing(currentElement) {
                    Name = "errors",
                });
            }
            CrawlTree(currentElement, AddErrorImportForEnums);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "github.com/microsoft/kiota/abstractions/go"),
            new ("HttpMethod", "github.com/microsoft/kiota/abstractions/go"),
            new ("RequestInfo", "github.com/microsoft/kiota/abstractions/go"),
            new ("ResponseHandler", "github.com/microsoft/kiota/abstractions/go"),
            new ("MiddlewareOption", "github.com/microsoft/kiota/abstractions/go"),
            new ("QueryParametersBase", "github.com/microsoft/kiota/abstractions/go"),
            new ("Parsable", "github.com/microsoft/kiota/abstractions/go/serialization"),
            new ("*url", "net/url"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "github.com/microsoft/kiota/abstractions/go/serialization"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "github.com/microsoft/kiota/abstractions/go/serialization"),
            new ("Parsable", "github.com/microsoft/kiota/abstractions/go/serialization"),
            new ("ConvertToArrayOfParsable", "github.com/microsoft/kiota/abstractions/go/serialization"),
            new ("ConvertToArrayOfPrimitives", "github.com/microsoft/kiota/abstractions/go/serialization"),
        };
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) &&
                currentMethod.Parent is CodeClass parentClass) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                    currentMethod.ReturnType.IsNullable = true;
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => {
                    x.Type.IsNullable = false;
                    x.Type.Name = "MiddlewareOption";
                    x.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
                });
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.QueryParameter)).ToList().ForEach(x => x.Type.Name = $"{parentClass.Name}{x.Type.Name}");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
                currentMethod.ReturnType.Name = "map[string]func(interface{}, i04eb5309aeaafadd28374d79c8471df9b267510b4dc2e3144c378c50f6fd7b55.ParseNode)(error)";
                currentMethod.Name = "getFieldDeserializers";
            } else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.HttpCore))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            else if(currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                currentMethod.ReturnType.IsNullable = true;
        }
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if (currentProperty.Type != null) {
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
                    currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
                }
            }
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
