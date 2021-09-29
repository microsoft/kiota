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
                defaultNamespaces);
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
        private static readonly Tuple<Func<CodeElement, bool>, string, string[]>[] defaultNamespaces = new Tuple<Func<CodeElement, bool>, string, string[]>[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.HttpCore),
                "github.com/microsoft/kiota/abstractions/go", new string[] {"HttpCore"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "github.com/microsoft/kiota/abstractions/go", new string[] {"RequestInformation", "HttpMethod", "MiddlewareOption"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "github.com/microsoft/kiota/abstractions/go", new string[] {"ResponseHandler"}),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.QueryParameters),
                "github.com/microsoft/kiota/abstractions/go", new string[] {"QueryParametersBase"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor) &&
                        !conventions.IsScalarType(method.ReturnType.Name) &&
                        !conventions.IsPrimitiveType(method.ReturnType.Name),
                "github.com/microsoft/kiota/abstractions/go/serialization", new string[] {"Parsable"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "github.com/microsoft/kiota/abstractions/go/serialization", new string[] {"SerializationWriter"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "github.com/microsoft/kiota/abstractions/go/serialization", new string[] {"ParseNode", "ConvertToArrayOfParsable", "ConvertToArrayOfPrimitives"}),
            new (x => x is CodeMethod method &&
                method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path) && "DateTimeOffset".Equals(x.Type.Name, StringComparison.OrdinalIgnoreCase)),
                "time", new string[] {"Time"}),
        };//TODO add backing store types once we have them defined
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
            } else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor))
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
                }
            }
        }
    }
}
