using System.Linq;
using System;
using Kiota.Builder.Extensions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Data;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override void Refine(CodeNamespace generatedCode)
    {
        AddDefaultImports(generatedCode, defaultUsingEvaluators);
        ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
        RemoveCancellationParameter(generatedCode);
        CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
        CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
        AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        AliasUsingsWithSameSymbol(generatedCode);
        AddParsableInheritanceForModelClasses(generatedCode, "Parsable");
        ReplaceBinaryByNativeType(generatedCode, "ArrayBuffer", null);
        ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
        AddGetterAndSetterMethods(generatedCode, new() {
                                                CodePropertyKind.Custom,
                                                CodePropertyKind.AdditionalData,
                                            }, _configuration.UsesBackingStore, false);
        AddConstructorsForDefaultValues(generatedCode, true);
        ReplaceDefaultSerializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory");
        ReplaceDefaultDeserializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonParseNodeFactory");
        AddSerializationModulesImport(generatedCode,
            new[] { $"{AbstractionsPackageName}.registerDefaultSerializer", 
                    $"{AbstractionsPackageName}.enableBackingStoreForSerializationWriterFactory",
                    $"{AbstractionsPackageName}.SerializationWriterFactoryRegistry"},
            new[] { $"{AbstractionsPackageName}.registerDefaultDeserializer",
                    $"{AbstractionsPackageName}.ParseNodeFactoryRegistry" });


        AddIndexModels(generatedCode);
    }

    private static void MoveAllModelsToTopLevel(CodeElement currentElement, CodeNamespace targetNamespace = null)
    {
        if (currentElement is CodeNamespace currentNamespace)
        {
            if (targetNamespace == null)
            {
                var rootModels = FindRootModelsNamespace(currentNamespace);
                targetNamespace = FindFirstModelSubnamepaceWithClasses(rootModels);
            }
            if (currentNamespace != targetNamespace &&
                !string.IsNullOrEmpty(currentNamespace.Name) &&
                currentNamespace.Name.Contains(targetNamespace.Name, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var codeClass in currentNamespace.Classes)
                {
                    currentNamespace.RemoveChildElement(codeClass);
                    targetNamespace.AddClass(codeClass);
                }
            }
            CrawlTree(currentElement, x => MoveAllModelsToTopLevel(x, targetNamespace));
        }
    }

    private static CodeNamespace FindRootModelsNamespace(CodeNamespace currentNamespace)
    {
        if (currentNamespace != null)
        {
            if (!string.IsNullOrEmpty(currentNamespace.Name) &&
                currentNamespace.Name.EndsWith("Models", StringComparison.OrdinalIgnoreCase))
                return currentNamespace;
            else
                foreach (var subNS in currentNamespace.Namespaces)
                {
                    var result = FindRootModelsNamespace(subNS);
                    if (result != null)
                        return result;
                }
        }
        return null;
    }

    private static CodeNamespace FindFirstModelSubnamepaceWithClasses(CodeNamespace currentNamespace)
    {
        if (currentNamespace != null)
        {
            if (currentNamespace.Classes.Any()) return currentNamespace;
            else
                foreach (var subNS in currentNamespace.Namespaces)
                {
                    var result = FindFirstModelSubnamepaceWithClasses(subNS);
                    if (result != null) return result;
                }
        }
        return null;
    }

    private static void AddIndexModels(CodeElement codeElement)
    {
        var orderedList = new List<string>();
        var set = new HashSet<string>();

        GenerateModelsIndex(codeElement, set, orderedList);
    
    }



    private static void GenerateModelsIndex(CodeElement codeElement, HashSet<string> parentSet, List<string> orderedList)
    {
       
        if (codeElement is CodeClass @class && @class.IsOfKind(CodeClassKind.Model))
        {

            var usings = @class.StartBlock as CodeClass.Declaration;
            var testc = usings.Inherits;
          //  var declaration = usings.
            if (@class.Parent != null)
            {
                if (!parentSet.Contains(codeElement.Name))
                {
                    parentSet.Add(codeElement.Name);

                    if (!parentSet.Contains(codeElement.Parent.Name))
                    {
                        orderedList.Insert(0, codeElement.Parent.Name);
                        parentSet.Add(codeElement.Parent.Name);
                    }
                    orderedList.Add(codeElement.Name);
                }

            }

           // var usings = @class.StartBlock.Usings;

            //  usings.Select(us => {us.Name })
        }
        
        
        CrawlTree(codeElement, c => GenerateModelsIndex(c,parentSet, orderedList));

    }
    private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
    private static void AliasUsingsWithSameSymbol(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass &&
            currentClass.StartBlock is CodeClass.Declaration currentDeclaration &&
            currentDeclaration.Usings.Any(x => !x.IsExternal)) {
                var duplicatedSymbolsUsings = currentDeclaration.Usings.Where(x => !x.IsExternal)
                                                                        .Distinct(usingComparer)
                                                                        .GroupBy(x => x.Declaration.Name, StringComparer.OrdinalIgnoreCase)
                                                                        .Where(x => x.Count() > 1)
                                                                        .SelectMany(x => x)
                                                                        .Union(currentDeclaration
                                                                                .Usings
                                                                                .Where(x => !x.IsExternal)
                                                                                .Where(x => x.Declaration
                                                                                                .Name
                                                                                                .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
                foreach(var usingElement in duplicatedSymbolsUsings)
                        usingElement.Alias = (usingElement.Declaration
                                                        .TypeDefinition
                                                        .GetImmediateParentOfType<CodeNamespace>()
                                                        .Name +
                                            usingElement.Declaration
                                                        .TypeDefinition
                                                        .Name)
                                            .GetNamespaceImportSymbol();
            }
        CrawlTree(currentElement, AliasUsingsWithSameSymbol);
    }
    private const string AbstractionsPackageName = "@microsoft/kiota-abstractions";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsPackageName, "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsPackageName, "HttpMethod", "RequestInformation", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            AbstractionsPackageName, "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            AbstractionsPackageName, "ParseNode"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
            AbstractionsPackageName, "getPathParameters"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            AbstractionsPackageName, "Parsable"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            AbstractionsPackageName, "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            AbstractionsPackageName, "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
    };
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
        else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
            currentProperty.Type.Name = "Map<string, unknown>";
            currentProperty.DefaultValue = "new Map<string, unknown>()";
        } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Record<string, unknown>";
            if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        } else
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "Record<string,RequestOption>");
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Headers)).ToList().ForEach(x => { x.Type.Name = "Record<string, string>"; x.Type.ActionOf = false; });
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = $"Map<string, (item: T, node: ParseNode) => void>";
        else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor)) {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if(urlTplParams != null &&
                urlTplParams.Type is CodeType originalType) {
                originalType.Name = "Record<string, unknown>";
                urlTplParams.Description = "The raw url or the Url template parameters for the request.";
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
        }
        CorrectDateTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new CodeTypeBase[] { currentMethod.ReturnType})
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("Date", null)},
    {"TimeSpan", ("Duration", new CodeUsing {
                                    Name = "Duration",
                                    Declaration = new CodeType {
                                        Name = AbstractionsPackageName,
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", ( null, new CodeUsing {
                            Name = "DateOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", ( null, new CodeUsing {
                            Name = "TimeOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    };
}
