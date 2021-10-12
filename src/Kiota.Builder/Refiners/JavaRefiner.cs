using System;
using System.Linq;
using Kiota.Builder.Writers.Java;

namespace Kiota.Builder.Refiners {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public JavaRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(generatedCode, false);
            InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, true);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddRawUrlConstructorOverload(generatedCode);
            ReplaceReservedNames(generatedCode, new JavaReservedNamesProvider(), x => $"{x}_escaped");
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            PatchHeaderParametersType(generatedCode, "Map<String, String>");
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true);
            AddEnumSetImport(generatedCode);
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                    CodePropertyKind.BackingStore,
                                                }, _configuration.UsesBackingStore, true);
            SetSetterParametersToNullable(generatedCode, new Tuple<CodeMethodKind, CodePropertyKind>(CodeMethodKind.Setter, CodePropertyKind.AdditionalData));
            AddConstructorsForDefaultValues(generatedCode, true);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            ReplaceDefaultSerializationModules(generatedCode, "com.microsoft.kiota.serialization.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "com.microsoft.kiota.serialization.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                                        new [] { "com.microsoft.kiota.ApiClientBuilder",
                                                "com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry" },
                                        new [] { "com.microsoft.kiota.serialization.ParseNodeFactoryRegistry" });
        }
        private static void SetSetterParametersToNullable(CodeElement currentElement, params Tuple<CodeMethodKind, CodePropertyKind>[] accessorPairs) {
            if(currentElement is CodeMethod method &&
                accessorPairs.Any(x => method.IsOfKind(x.Item1) && (method.AccessedProperty?.IsOfKind(x.Item2) ?? false))) 
                foreach(var param in method.Parameters)
                    param.Type.IsNullable = true;
            CrawlTree(currentElement, element => SetSetterParametersToNullable(element, accessorPairs));   
        }
        private static void AddEnumSetImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) &&
                currentClass.Properties.Any(x => x.Type is CodeType xType && xType.TypeDefinition is CodeEnum xEnumType && xEnumType.Flags)) {
                    var nUsing = new CodeUsing {
                        Name = "EnumSet",
                        Declaration = new CodeType { Name = "java.util", IsExternal = true },
                    };
                    currentClass.AddUsing(nUsing);
                }

            CrawlTree(currentElement, AddEnumSetImport);
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.AddImplements(new CodeType {
                    IsExternal = true,
                    Name = $"Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly JavaConventionService conventionService = new();
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "com.microsoft.kiota", "RequestAdapter"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.UrlTemplateParameters),
                "java.util", "HashMap"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "com.microsoft.kiota", "RequestInformation", "RequestOption", "HttpMethod"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "java.net", "URISyntaxException"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "java.util", "Collection", "Map"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "com.microsoft.kiota", "ResponseHandler"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.QueryParameters),
                "com.microsoft.kiota", "QueryParametersBase"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "com.microsoft.kiota.serialization", "Parsable"),
            new (x => x is CodeMethod method && method.Parameters.Any(x => !x.Optional),
                    "java.util", "Objects"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor) && 
                        method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.RequestBody) &&
                                            x.Type.Name.Equals(conventionService.StreamTypeName, StringComparison.OrdinalIgnoreCase)),
                "java.io", "InputStream"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "com.microsoft.kiota.serialization", "SerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "com.microsoft.kiota.serialization", "ParseNode"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "com.microsoft.kiota.serialization", "Parsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "java.util.function", "BiConsumer"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "java.util", "HashMap", "Map"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                        method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "com.microsoft.kiota.store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "com.microsoft.kiota.store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton"),
        };
        private const string OriginalDateTimeOffsetType = "DateTimeOffset";
        private const string JavaOffsetDateTimeType = "OffsetDateTime";
        private const string JavaOffsetDateTimeTypePackage = "java.time";
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter)) {
                currentProperty.Type.Name = "RequestAdapter";
                currentProperty.Type.IsNullable = true;
            }
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if(OriginalDateTimeOffsetType.Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase)) {
                currentProperty.Type.Name = JavaOffsetDateTimeType;
                var nUsing = new CodeUsing {
                    Name = JavaOffsetDateTimeType,
                    Declaration = new CodeType {
                        Name = JavaOffsetDateTimeTypePackage,
                        IsExternal = true,
                    },
                };
                
                (currentProperty.Parent as CodeClass).AddUsing(nUsing);
            } else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "Map<String, Object>";
                currentProperty.DefaultValue = "new HashMap<>()";
            } else if(currentProperty.IsOfKind(CodePropertyKind.UrlTemplate)) {
                currentProperty.Type.IsNullable = true;
            } else if(currentProperty.IsOfKind(CodePropertyKind.UrlTemplateParameters)) {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "HashMap<String, String>";
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = "new HashMap<>()";
            }
        }
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "Collection<RequestOption>");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer)).ToList().ForEach(x => {
                    x.Optional = false;
                    x.Type.IsNullable = true;
                    if(x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase))
                        x.Type.Name = x.Type.Name[1..];
                });
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
                currentMethod.ReturnType.Name = $"Map<String, BiConsumer<T, ParseNode>>";
                currentMethod.Name = "getFieldDeserializers";
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.UrlTemplateParameters))
                    .ToList()
                    .ForEach(x => x.Type.IsNullable = true);
                var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.UrlTemplateParameters));
                if(urlTplParams != null) 
                    urlTplParams.Type.Name = "HashMap<String, String>";
            }
            if (currentMethod.IsOfKind(CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.Constructor) &&
                    currentMethod.Parameters.Any(x => OriginalDateTimeOffsetType.Equals(x.Type.Name, StringComparison.OrdinalIgnoreCase)) &&
                    currentMethod.Parent is CodeClass parentClass) {
                currentMethod.Parameters.Where(x => OriginalDateTimeOffsetType.Equals(x.Type.Name, StringComparison.OrdinalIgnoreCase))
                                        .ToList()
                                        .ForEach(x => x.Type.Name = JavaOffsetDateTimeType);
                var nUsing = new CodeUsing {
                    Name = JavaOffsetDateTimeType,
                    Declaration = new CodeType {
                        Name = JavaOffsetDateTimeTypePackage,
                        IsExternal = true,
                    },
                };
                parentClass.AddUsing(nUsing);
            }
        }
        private static void InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var codeMethods = currentClass.Methods;
                if(codeMethods.Any()) {
                    var originalExecutorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
                    var executorMethodsToAdd = originalExecutorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.ResponseHandler))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Headers, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Where(x => x != null);
                    var originalGeneratorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestGenerator));
                    var generatorMethodsToAdd = originalGeneratorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.Options))
                                        .Union(originalGeneratorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Headers, CodeParameterKind.Options)))
                                        .Union(originalGeneratorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.Options)))
                                        .Where(x => x != null);
                    var originalConstructors = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.Constructor));
                    if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                        currentClass.AddMethod(executorMethodsToAdd
                                                .Union(generatorMethodsToAdd)
                                                .ToArray());
                }
            }
            
            CrawlTree(currentElement, InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors);
        }
        private static CodeMethod GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude) {
            if(currentMethod.Parameters.Any(x => x.IsOfKind(parameterTypesToExclude))) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.RemoveParametersByKind(parameterTypesToExclude);
                cloneMethod.OriginalMethod = currentMethod;
                return cloneMethod;
            }
            else return null;
        }
    }
}
