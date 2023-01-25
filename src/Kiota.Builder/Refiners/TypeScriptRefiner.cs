using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() => {
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode, false, "ById");
            RemoveCancellationParameter(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            cancellationToken.ThrowIfCancellationRequested();
            AddInnerClasses(generatedCode, 
                true, 
                string.Empty,
                true);
            // `AddInnerClasses` will have inner classes moved to their own files, so  we add the imports after so that the files don't miss anything.
            // This is because imports are added at the file level so nested classes would potentially use the higher level imports.
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            DisableActionOf(generatedCode, 
                CodeParameterKind.RequestConfiguration);
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "ArrayBuffer", string.Empty, isNullable: true);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                },
                _configuration.UsesBackingStore,
                false,
                string.Empty,
                string.Empty);
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new (StringComparer.OrdinalIgnoreCase) {
                    "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory",
                    "@microsoft/kiota-serialization-text.TextSerializationWriterFactory",
                    "@microsoft/kiota-serialization-form.FormSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new (StringComparer.OrdinalIgnoreCase) {
                    "@microsoft/kiota-serialization-json.JsonParseNodeFactory",
                    "@microsoft/kiota-serialization-text.TextParseNodeFactory",
                    "@microsoft/kiota-serialization-form.FormParseNodeFactory",
                }
            );
            AddSerializationModulesImport(generatedCode,
                new[] { $"{AbstractionsPackageName}.registerDefaultSerializer", 
                        $"{AbstractionsPackageName}.enableBackingStoreForSerializationWriterFactory",
                        $"{AbstractionsPackageName}.SerializationWriterFactoryRegistry"},
                new[] { $"{AbstractionsPackageName}.registerDefaultDeserializer",
                        $"{AbstractionsPackageName}.ParseNodeFactoryRegistry" });
            cancellationToken.ThrowIfCancellationRequested();
            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiError",
                    "@microsoft/kiota-abstractions"
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: false
            );
            static string factoryNameCallbackFromTypeName(string? x) => $"create{x.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
            ReplaceLocalMethodsByGlobalFunctions(
                generatedCode,
                static x => factoryNameCallbackFromTypeName(x.Parent?.Name),
                static x => { 
                    var result = new List<CodeUsing>() {
                        new() { Name = "ParseNode", Declaration = new() { Name = AbstractionsPackageName, IsExternal = true } }
                    };
                    if (x.Parent?.Parent != null)
                        result.Add(new() { Name = x.Parent.Parent.Name, Declaration = new() { Name = x.Parent.Name, TypeDefinition = x.Parent } });

                    if (x.Parent is CodeClass parentClass && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
                        result.AddRange(parentClass.DiscriminatorInformation
                                        .DiscriminatorMappings
                                        .Select(static y => y.Value)
                                        .OfType<CodeType>()
                                        .Select(static y => new CodeUsing { Name = y.Name, Declaration = y }));
                    return result.ToArray();
                },
                CodeMethodKind.Factory
            );
            static string factoryNameCallbackFromType(CodeType x) => factoryNameCallbackFromTypeName(x.Name);
            cancellationToken.ThrowIfCancellationRequested();
            AddStaticMethodsUsingsForDeserializer(
                generatedCode,
                factoryNameCallbackFromType
            );
            AddStaticMethodsUsingsForRequestExecutor(
                generatedCode,
                factoryNameCallbackFromType
            );
            AliasUsingsWithSameSymbol(generatedCode);
            AddQueryParameterMapperMethod(
                generatedCode
            );
        }, cancellationToken);
    }
    private static void AliasCollidingSymbols(IEnumerable<CodeUsing> usings, string currentSymbolName)
    {
        var duplicatedSymbolsUsings = usings.Where(static x => !x.IsExternal)
                                                                .Where(static x => x.Declaration != null && x.Declaration.TypeDefinition != null)
                                                                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                                                                .Where(static x => x.DistinctBy(static y => y.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>())
                                                                                    .Count() > 1)
                                                                .SelectMany(static x => x)
                                                                .Union(usings
                                                                        .Where(static x => !x.IsExternal && x.Declaration != null)
                                                                        .Where(x => x.Declaration!
                                                                                        .Name
                                                                                        .Equals(currentSymbolName, StringComparison.OrdinalIgnoreCase)))
                                                                .ToArray();
        foreach (var usingElement in duplicatedSymbolsUsings)
            usingElement.Alias = (usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.GetImmediateParentOfType<CodeNamespace>()
                                            .Name +
                                usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.Name)
                                .GetNamespaceImportSymbol()
                                .ToFirstCharacterUpperCase();
    }
    private static void AliasUsingsWithSameSymbol(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration &&
            currentDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(currentDeclaration.Usings, currentClass.Name);
        }
        else if (currentElement is CodeFunction currentFunction &&
                    currentFunction.StartBlock is BlockDeclaration currentFunctionDeclaration &&
                    currentFunctionDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(currentFunctionDeclaration.Usings, currentFunction.Name);
        } else if (currentElement is CodeInterface currentInterface && 
                    currentInterface.StartBlock is InterfaceDeclaration interfaceDeclaration &&
                    interfaceDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(interfaceDeclaration.Usings, currentInterface.Name);
        }
        CrawlTree(currentElement, AliasUsingsWithSameSymbol);
    }
    private const string AbstractionsPackageName = "@microsoft/kiota-abstractions";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = { 
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsPackageName, "RequestAdapter"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Options),
            AbstractionsPackageName, "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsPackageName, "HttpMethod", "RequestInformation", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            AbstractionsPackageName, "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            AbstractionsPackageName, "ParseNode"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
            AbstractionsPackageName, "getPathParameters"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "Parsable", "ParsableFactory"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            AbstractionsPackageName, "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            AbstractionsPackageName, "AdditionalDataHolder"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            AbstractionsPackageName, "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            AbstractionsPackageName, "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
    };
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
        else if(currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.Type.Name = "RequestOption[]";
        else if(currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.Type.Name = "Record<string, string[]>";
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Record<string, unknown>";
            currentProperty.DefaultValue = "{}";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Record<string, unknown>";
            if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = "Record<string, (node: ParseNode) => void>";
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if(urlTplParams != null &&
                urlTplParams.Type is CodeType originalType) {
                originalType.Name = "Record<string, unknown>";
                urlTplParams.Documentation.Description = "The raw url or the Url template parameters for the request.";
                var unionType = new CodeUnionType {
                    Name = "rawUrlOrTemplateParameters",
                    IsNullable = true,
                };
                unionType.AddType(originalType, new() {
                    Name = "string",
                    IsNullable = true,
                    IsExternal = true,
                });
                urlTplParams.Type = unionType;
            }
        } else if(currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType})
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("Date", null)},
    {"TimeSpan", ("Duration", new CodeUsing {
                                    Name = "Duration",
                                    Declaration = new CodeType {
                                        Name = AbstractionsPackageName,
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", (string.Empty, new CodeUsing {
                            Name = "DateOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", (string.Empty, new CodeUsing {
                            Name = "TimeOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    };
}
