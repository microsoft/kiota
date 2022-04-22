﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Java;

namespace Kiota.Builder.Refiners;
public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public JavaRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override void Refine(CodeNamespace generatedCode)
    {
        LowerCaseNamespaceNames(generatedCode);
        AddInnerClasses(generatedCode, false, string.Empty);
        InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(generatedCode);
        ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, true);
        RemoveCancellationParameter(generatedCode);
        ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
        AddRawUrlConstructorOverload(generatedCode);
        CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
        ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true);
        AddGetterAndSetterMethods(generatedCode,
            new() {
                CodePropertyKind.Custom,
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore,
            },
            _configuration.UsesBackingStore,
            true,
            "get",
            "set"
        );
        ReplaceReservedNames(generatedCode, new JavaReservedNamesProvider(), x => $"{x}_escaped");
        AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
        AddDefaultImports(generatedCode, defaultUsingEvaluators);
        AddParsableImplementsForModelClasses(generatedCode, "Parsable");
        AddEnumSetImport(generatedCode);
        SetSetterParametersToNullable(generatedCode, new Tuple<CodeMethodKind, CodePropertyKind>(CodeMethodKind.Setter, CodePropertyKind.AdditionalData));
        AddConstructorsForDefaultValues(generatedCode, true);
        CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
        ReplaceDefaultSerializationModules(
            generatedCode,
            "com.microsoft.kiota.serialization.JsonSerializationWriterFactory",
            "com.microsoft.kiota.serialization.TextSerializationWriterFactory"
        );
        ReplaceDefaultDeserializationModules(
            generatedCode,
            "com.microsoft.kiota.serialization.JsonParseNodeFactory",
            "com.microsoft.kiota.serialization.TextParseNodeFactory"
        );
        AddSerializationModulesImport(generatedCode,
                                    new [] { "com.microsoft.kiota.ApiClientBuilder",
                                            "com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry" },
                                    new [] { "com.microsoft.kiota.serialization.ParseNodeFactoryRegistry" });
        AddParentClassToErrorClasses(
                generatedCode,
                "ApiException",
                "com.microsoft.kiota"
        );
        AddDiscriminatorMappingsUsingsToParentClasses(
            generatedCode,
            "ParseNode",
            addUsings: false
        );
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
    private static readonly JavaConventionService conventionService = new();
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] {
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "com.microsoft.kiota", "RequestAdapter"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.PathParameters),
            "java.util", "HashMap"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "com.microsoft.kiota", "RequestInformation", "RequestOption", "HttpMethod"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "java.net", "URISyntaxException"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "java.util", "Collection", "Map"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "com.microsoft.kiota", "ResponseHandler"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            "com.microsoft.kiota.serialization", "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            "com.microsoft.kiota.serialization", "AdditionalDataHolder"),
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
            "com.microsoft.kiota.serialization", "Parsable", "ParsableFactory"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "java.util.function", "Consumer"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "java.util", "HashMap", "Map"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "com.microsoft.kiota.store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            "com.microsoft.kiota.store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Options),
            "java.util", "Collections"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            "java.util", "HashMap"),
        new (x => x is CodeProperty prop && "decimal".Equals(prop.Type.Name, StringComparison.OrdinalIgnoreCase) ||
                x is CodeMethod method && "decimal".Equals(method.ReturnType.Name, StringComparison.OrdinalIgnoreCase) ||
                x is CodeParameter para && "decimal".Equals(para.Type.Name, StringComparison.OrdinalIgnoreCase),
            "java.math", "BigDecimal"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(prop.SerializationName),
                "com.microsoft.kiota", "QueryParameter"),
    };
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter)) {
            currentProperty.Type.Name = "RequestAdapter";
            currentProperty.Type.IsNullable = true;
        }
        else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
        else if (currentProperty.IsOfKind(CodePropertyKind.Options)) {
            currentProperty.Type.Name = "Collection<RequestOption>";
            currentProperty.DefaultValue = "Collections.emptyList()";
        } else if (currentProperty.IsOfKind(CodePropertyKind.Headers)) {
            currentProperty.Type.Name = "HashMap<String, String>";
            currentProperty.DefaultValue = "new HashMap<>()";
        } else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameter))
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
            currentProperty.Type.Name = "Map<String, Object>";
            currentProperty.DefaultValue = "new HashMap<>()";
        } else if(currentProperty.IsOfKind(CodePropertyKind.UrlTemplate)) {
            currentProperty.Type.IsNullable = true;
        } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "HashMap<String, Object>";
            if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "new HashMap<>()";
        } else
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer)).ToList().ForEach(x => {
                x.Optional = false;
                x.Type.IsNullable = true;
                if(x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase))
                    x.Type.Name = x.Type.Name[1..];
            });
        else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
            currentMethod.ReturnType.Name = $"Map<String, Consumer<ParseNode>>";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters))
                .ToList()
                .ForEach(x => x.Type.IsNullable = true);
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if(urlTplParams != null)
                urlTplParams.Type.Name = "HashMap<String, Object>";
        }
        CorrectDateTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new CodeTypeBase[] { currentMethod.ReturnType})
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("OffsetDateTime", new CodeUsing {
                                    Name = "OffsetDateTime",
                                    Declaration = new CodeType {
                                        Name = "java.time",
                                        IsExternal = true,
                                    },
                                })},
    {"TimeSpan", ("Period", new CodeUsing {
                                    Name = "Period",
                                    Declaration = new CodeType {
                                        Name = "java.time",
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", ("LocalDate", new CodeUsing {
                            Name = "LocalDate",
                            Declaration = new CodeType {
                                Name = "java.time",
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", ("LocalTime", new CodeUsing {
                            Name = "LocalTime",
                            Declaration = new CodeType {
                                Name = "java.time",
                                IsExternal = true,
                            },
                        })},
    };
    private void InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass) {
            var codeMethods = currentClass.Methods;
            if(codeMethods.Any()) {
                var originalExecutorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
                var executorMethodsToAdd = originalExecutorMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.ResponseHandler))
                                    .Union(originalExecutorMethods
                                            .Select(x => GetMethodClone(x, CodeParameterKind.RequestConfiguration, CodeParameterKind.ResponseHandler)))
                                    .Where(x => x != null);
                var originalGeneratorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestGenerator));
                var generatorMethodsToAdd = originalGeneratorMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.RequestConfiguration))
                                    .Where(x => x != null);
                if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                    currentClass.AddMethod(executorMethodsToAdd
                                            .Union(generatorMethodsToAdd)
                                            .ToArray());
            }
        }

        CrawlTree(currentElement, InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors);
    }

    // Namespaces in Java by convention are all lower case, like:
    // com.microsoft.kiota.serialization
    private static void LowerCaseNamespaceNames(CodeElement currentElement) {
        if (currentElement is CodeNamespace codeNamespace)
        {
            if (!string.IsNullOrEmpty(codeNamespace.Name))
                codeNamespace.Name = codeNamespace.Name.ToLowerInvariant();

            CrawlTree(currentElement, LowerCaseNamespaceNames);
        }
    }
}
