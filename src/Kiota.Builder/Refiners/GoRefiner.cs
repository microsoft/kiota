using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Refiners;
public class GoRefiner : CommonLanguageRefiner
{
    public GoRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override void Refine(CodeNamespace generatedCode)
    {
        _configuration.NamespaceNameSeparator = "/";
        AddInnerClasses(
            generatedCode,
            true,
            null);
        ReplaceIndexersByMethodsWithParameter(
            generatedCode,
            generatedCode,
            false,
            "ById");
        RemoveCancellationParameter(generatedCode);
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
        RemoveModelPropertiesThatDependOnSubNamespaces(
            generatedCode
        );
        ReplaceReservedNames(
            generatedCode,
            new GoReservedNamesProvider(),
            x => $"{x}_escaped",
            shouldReplaceCallback: x => x is not CodeProperty currentProp || 
                                        !(currentProp.Parent is CodeClass parentClass &&
                                        parentClass.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.ParameterSet) &&
                                        currentProp.Access == AccessModifier.Public)); // Go reserved keywords are all lowercase and public properties are uppercased when we don't provide accessors (models)
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
            CorrectPropertyType,
            CorrectImplements);
        InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(generatedCode);
        DisableActionOf(generatedCode, 
            CodeParameterKind.RequestConfiguration);
        AddGetterAndSetterMethods(
            generatedCode, 
            new () { 
                CodePropertyKind.AdditionalData,
                CodePropertyKind.Custom,
                CodePropertyKind.BackingStore }, 
            _configuration.UsesBackingStore,
            false,
            "Get",
            "Set");
        AddConstructorsForDefaultValues(
            generatedCode,
            true,
            true,  //forcing add as constructors are required for by factories 
            new CodeClassKind[] { CodeClassKind.RequestConfiguration });
        MakeModelPropertiesNullable(
            generatedCode);
        AddErrorImportForEnums(
            generatedCode);
        ReplaceDefaultSerializationModules(
            generatedCode,
            "github.com/microsoft/kiota-serialization-json-go.JsonSerializationWriterFactory",
            "github.com/microsoft/kiota-serialization-text-go.TextSerializationWriterFactory");
        ReplaceDefaultDeserializationModules(
            generatedCode,
            "github.com/microsoft/kiota-serialization-json-go.JsonParseNodeFactory",
            "github.com/microsoft/kiota-serialization-text-go.TextParseNodeFactory");
        AddSerializationModulesImport(
            generatedCode,
            new string[] {"github.com/microsoft/kiota-abstractions-go/serialization.SerializationWriterFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultSerializer"},
            new string[] {"github.com/microsoft/kiota-abstractions-go/serialization.ParseNodeFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultDeserializer"});
        AddParentClassToErrorClasses(
                generatedCode,
                "ApiError",
                "github.com/microsoft/kiota-abstractions-go"
        );
        AddDiscriminatorMappingsUsingsToParentClasses(
            generatedCode,
            "ParseNode",
            true
        );
        AddParsableImplementsForModelClasses(
            generatedCode,
            "Parsable"
        );
        CopyModelClassesAsInterfaces(
            generatedCode,
            x => $"{x.Name}able"
        );
    }
    private void InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass) {
            var codeMethods = currentClass.Methods;
            if(codeMethods.Any(x => x.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator))) {
                var originalExecutorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor)).ToList();
                var executorMethodsToAdd = originalExecutorMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.RequestConfiguration, CodeParameterKind.ResponseHandler))
                                    .Where(x => x != null)
                                    .ToArray();//otherwise the name change also affects the clones
                var originalGeneratorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestGenerator)).ToList();
                var generatorMethodsToAdd = originalGeneratorMethods
                                    .Select(x => GetMethodClone(x, CodeParameterKind.RequestConfiguration))
                                    .Where(x => x != null)
                                    .ToArray();
                originalExecutorMethods.ForEach(x => x.Name = $"{x.Name}With{nameof(CodeParameterKind.RequestConfiguration)}And{nameof(CodeParameterKind.ResponseHandler)}");
                originalGeneratorMethods.ForEach(x => x.Name = $"{x.Name}With{nameof(CodeParameterKind.RequestConfiguration)}");
                if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                    currentClass.AddMethod(executorMethodsToAdd
                                            .Union(generatorMethodsToAdd)
                                            .ToArray());
            }
        }

        CrawlTree(currentElement, InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors);
    }
    private static void RemoveModelPropertiesThatDependOnSubNamespaces(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass && 
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Parent is CodeNamespace currentNamespace) {
            var propertiesToRemove = currentClass.Properties
                                                    .Where(x => x.IsOfKind(CodePropertyKind.Custom) &&
                                                                x.Type is CodeType pType &&
                                                                !pType.IsExternal &&
                                                                pType.TypeDefinition != null &&
                                                                currentNamespace.IsParentOf(pType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>()))
                                                    .ToArray();
            if(propertiesToRemove.Any()) {
                currentClass.RemoveChildElement(propertiesToRemove);
                var propertiesToRemoveHashSet = propertiesToRemove.ToHashSet();
                var methodsToRemove = currentClass.Methods
                                                    .Where(x => x.IsAccessor &&
                                                            propertiesToRemoveHashSet.Contains(x.AccessedProperty))
                                                    .ToArray();
                currentClass.RemoveChildElement(methodsToRemove);
            }
        }
        CrawlTree(currentElement, RemoveModelPropertiesThatDependOnSubNamespaces);
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
                    Kind = CodeMethodKind.RequestBuilderBackwardCompatibility,
                });
            }
        CrawlTree(currentElement, ReplaceRequestBuilderPropertiesByMethods);
    }
    private static void AddErrorImportForEnums(CodeElement currentElement) {
        if(currentElement is CodeEnum currentEnum) {
            currentEnum.AddUsing(new CodeUsing {
                Name = "errors",
            });
        }
        CrawlTree(currentElement, AddErrorImportForEnums);
    }
    private static readonly GoConventionService conventions = new();
    private static readonly HashSet<string> typeToSkipStrConv = new(StringComparer.OrdinalIgnoreCase) {
        "DateTimeOffset",
        "Duration",
        "TimeOnly",
        "DateOnly",
        "string"
    };
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "github.com/microsoft/kiota-abstractions-go", "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "github.com/microsoft/kiota-abstractions-go", "RequestInformation", "HttpMethod", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "github.com/microsoft/kiota-abstractions-go", "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor) &&
                    method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path) &&
                                            !typeToSkipStrConv.Contains(x.Type.Name)),
            "strconv", "FormatBool"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "github.com/microsoft/kiota-abstractions-go/serialization", "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            "github.com/microsoft/kiota-abstractions-go/serialization", "ParseNode", "Parsable"),
        new (x => x is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model),
            "github.com/microsoft/kiota-abstractions-go/serialization", "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && 
                                            (@class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)) ||
                                            @class.StartBlock.Implements.Any(x => KiotaBuilder.AdditionalHolderInterface.Equals(x.Name, StringComparison.OrdinalIgnoreCase))),
            "github.com/microsoft/kiota-abstractions-go/serialization", "AdditionalDataHolder"),
    };//TODO add backing store types once we have them defined
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        var parentClass = currentMethod.Parent as CodeClass;
        if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) &&
            parentClass != null) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => {
                    x.Type.Name = "ResponseHandler";
                    x.Type.IsNullable = false; //no pointers
                });
            else if(currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                currentMethod.ReturnType.IsNullable = true;
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
        else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
            currentMethod.ReturnType.Name = $"map[string]func({conventions.SerializationHash}.ParseNode)(error)";
            currentMethod.Name = "getFieldDeserializers";
        } else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
            var rawUrlParam = currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl);
            if(rawUrlParam != null)
                rawUrlParam.Type.IsNullable = false;
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        } else if(currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.Factory)) {
            currentMethod.ReturnType.IsNullable = true;
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ParseNode)).ToList().ForEach(x => x.Type.IsNullable = false);
            if(currentMethod.IsOfKind(CodeMethodKind.Factory))
                currentMethod.ReturnType = new CodeType { Name = "Parsable", IsNullable = false, IsExternal = true };
        }
        CorrectDateTypes(parentClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new CodeTypeBase[] { currentMethod.ReturnType})
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
        {"DateTimeOffset", ("Time", new CodeUsing {
                                        Name = "Time",
                                        Declaration = new CodeType {
                                            Name = "time",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("ISODuration", new CodeUsing {
                                        Name = "ISODuration",
                                        Declaration = new CodeType {
                                            Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", (null, new CodeUsing {
                                Name = "DateOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", (null, new CodeUsing {
                                Name = "TimeOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if (currentProperty.Type != null) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
                currentProperty.Type.Name = "RequestAdapter";
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "map[string]interface{}";
                currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "map[string]string";
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.Headers)) {
                currentProperty.Type.Name = "map[string]string";
                currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.Options)) {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "RequestOption";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            } else
                CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        }
    }
}
