using System;
using System.Linq;
using Kiota.Builder.Writers.Go;

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
                false,
                "ById");
            ReplaceRequestBuilderPropertiesByMethods(
                generatedCode
            );
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore
            );
            AddRawUrlConstructorOverload(
                generatedCode
            );
            MoveAllModelsToTopLevel(
                generatedCode
            );
            ReplaceReservedNames(
                generatedCode,
                new GoReservedNamesProvider(),
                x => $"{x}_escpaped");
            AddPropertiesAndMethodTypesImports(
                generatedCode,
                true,
                false,
                true);
            AddDefaultImports(
                generatedCode,
                defaultUsingEvaluators);
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
        private static void MoveAllModelsToTopLevel(CodeElement currentElement, CodeNamespace targetNamespace = null) {
            if(currentElement is CodeNamespace currentNamespace) {
                if(targetNamespace == null) {
                    var rootModels = FindRootModelsNamespace(currentNamespace);
                    targetNamespace = FindFirstModelSubnamepaceWithClasses(rootModels);
                }
                if(currentNamespace != targetNamespace &&
                    !string.IsNullOrEmpty(currentNamespace.Name) &&
                    currentNamespace.Name.Contains(targetNamespace.Name, StringComparison.OrdinalIgnoreCase)) {
                    foreach (var codeClass in currentNamespace.Classes)
                    {
                        currentNamespace.RemoveChildElement(codeClass);
                        targetNamespace.AddClass(codeClass);
                    }
                }
                CrawlTree(currentElement, x => MoveAllModelsToTopLevel(x, targetNamespace));
            }
        }
        private static CodeNamespace FindFirstModelSubnamepaceWithClasses(CodeNamespace currentNamespace) {
            if(currentNamespace != null) {
                if(currentNamespace.Classes.Any()) return currentNamespace;
                else
                    foreach (var subNS in currentNamespace.Namespaces)
                    {
                        var result = FindFirstModelSubnamepaceWithClasses(subNS);
                        if (result != null) return result;
                    }
            }
            return null;
        }
        private static CodeNamespace FindRootModelsNamespace(CodeNamespace currentNamespace) {
            if(currentNamespace != null) {
                if(!string.IsNullOrEmpty(currentNamespace.Name) &&
                    currentNamespace.Name.EndsWith("Models", StringComparison.OrdinalIgnoreCase))
                    return currentNamespace;
                else
                    foreach(var subNS in currentNamespace.Namespaces)
                    {
                        var result = FindRootModelsNamespace(subNS);
                        if(result != null)
                            return result;
                    }
            }
            return null;
        }
        private static void ReplaceRequestBuilderPropertiesByMethods(CodeElement currentElement) {
            if(currentElement is CodeProperty currentProperty &&
                currentProperty.IsOfKind(CodePropertyKind.RequestBuilder) &&
                currentElement.Parent is CodeClass parentClass) {
                    parentClass.RemoveChildElement(currentProperty);
                    currentProperty.Type.IsNullable = false;
                    parentClass.AddMethod(new CodeMethod {
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
                currentEnum.AddUsings(new CodeUsing {
                    Name = "errors",
                });
            }
            CrawlTree(currentElement, AddErrorImportForEnums);
        }
        private static readonly GoConventionService conventions = new();
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "github.com/microsoft/kiota/abstractions/go", "RequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "github.com/microsoft/kiota/abstractions/go", "RequestInformation", "HttpMethod", "RequestOption"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "github.com/microsoft/kiota/abstractions/go", "ResponseHandler"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.QueryParameters),
                "github.com/microsoft/kiota/abstractions/go", "QueryParametersBase"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor) &&
                        !conventions.IsScalarType(method.ReturnType.Name) &&
                        !conventions.IsPrimitiveType(method.ReturnType.Name),
                "github.com/microsoft/kiota/abstractions/go/serialization", "Parsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor) &&
                        method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path) &&
                                                !x.Type.Name.Equals("string", StringComparison.OrdinalIgnoreCase) &&
                                                !x.Type.Name.Equals("DateTimeOffset", StringComparison.OrdinalIgnoreCase)),
                "strconv", "FormatBool"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "github.com/microsoft/kiota/abstractions/go/serialization", "SerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "github.com/microsoft/kiota/abstractions/go/serialization", "ParseNode", "ConvertToArrayOfParsable", "ConvertToArrayOfPrimitives"),
            new (x => x is CodeMethod method &&
                method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path) && "DateTimeOffset".Equals(x.Type.Name, StringComparison.OrdinalIgnoreCase)),
                "time", "Time"),
            new (x => x is CodeEnum num, "ToUpper", "strings"),
        };//TODO add backing store types once we have them defined
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) &&
                currentMethod.Parent is CodeClass parentClass) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => {
                        x.Type.Name = "ResponseHandler";
                        x.Type.IsNullable = false; //no pointers
                    });
                else if(currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                    currentMethod.ReturnType.IsNullable = true;
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => {
                    x.Type.IsNullable = false;
                    x.Type.Name = "RequestOption";
                    x.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
                });
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.QueryParameter)).ToList().ForEach(x => x.Type.Name = $"{parentClass.Name}{x.Type.Name}");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
                currentMethod.ReturnType.Name = $"map[string]func(interface{{}}, {conventions.SerializationHash}.ParseNode)(error)";
                currentMethod.Name = "getFieldDeserializers";
            } else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
                var rawUrlParam = currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl);
                if(rawUrlParam != null)
                    rawUrlParam.Type.IsNullable = false;
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            } else if(currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility)) {
                currentMethod.ReturnType.IsNullable = true;
            }
        }
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if (currentProperty.Type != null) {
                if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
                    currentProperty.Type.Name = "RequestAdapter";
                else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                    currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
                else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase)) {
                    currentProperty.Type.Name = "Time";
                    var nUsing = new CodeUsing {
                        Name = "Time",
                        Declaration = new CodeType {
                            Name = "time",
                            IsExternal = true,
                        },
                    };
                    (currentProperty.Parent as CodeClass).AddUsing(nUsing);
                } else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                    currentProperty.Type.Name = "map[string]interface{}";
                    currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
                } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                    currentProperty.Type.IsNullable = true;
                    currentProperty.Type.Name = "map[string]string";
                    if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                        currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
                }
            }
        }
    }
}
