using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;
using Kiota.Builder.Writers.Ruby;

namespace Kiota.Builder.Refiners;

public partial class RubyRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public RubyRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlConstructor);
            DeduplicateErrorMappings(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by_{x.ToSnakeCase()}",
                static x => x.ToSnakeCase(),
                GenerationLanguage.Ruby);
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "MicrosoftKiotaAbstractions::BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = "MicrosoftKiotaAbstractions",
                        IsExternal = true
                    }
                });
            RemoveRequestConfigurationClasses(generatedCode);
            DisambiguateClassesWithNamespaceNames(generatedCode, "Model");
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s,
                false
            );
            var reservedNamesProvider = new RubyReservedNamesProvider();
            CorrectNames(generatedCode, s =>
            {
                if (s.Contains('_', StringComparison.OrdinalIgnoreCase) &&
                     s.ToPascalCase(UnderscoreArray) is string refinedName &&
                    !reservedNamesProvider.ReservedNames.Contains(s) &&
                    !reservedNamesProvider.ReservedNames.Contains(refinedName))
                    return refinedName;
                else
                    return s;
            }, false, true);
            cancellationToken.ThrowIfCancellationRequested();
            if (generatedCode.FindNamespaceByName(_configuration.ModelsNamespaceName) is CodeNamespace modelsNS)
                FlattenModelsNamespaces(modelsNS, modelsNS);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, true);
            // a type in the same or a parent namespace is not always reachable through a barrel
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true, static x => x.Where(static y => y.AllTypes.Any(static z => z.TypeDefinition is CodeClass { Kind: CodeClassKind.Model or CodeClassKind.RequestBuilder } or CodeEnum)));
            RemoveCancellationParameter(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddParsableImplementsForModelClasses(generatedCode, "MicrosoftKiotaAbstractions::Parsable");
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            RemoveUntypedNodeTypeValues(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "StringIO", "stringio", true);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            ReplacePropertyNames(generatedCode,
                [
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                ],
                static s => s.ToSnakeCase());
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiError",
                "MicrosoftKiotaAbstractions",
                true
            );
            AddPrimaryErrorMessage(generatedCode,
                "message",
                () => new CodeType { Name = "string", IsNullable = false, IsExternal = true },
                true
            );
            ReplaceReservedNames(generatedCode, reservedNamesProvider, x => $"{x}_escaped");
            // Ruby inherits initialize, so a subclass needs one only for defaults of its own
            AddConstructorsForDefaultValues(
                generatedCode,
                false,
                false,
                [CodeClassKind.RequestConfiguration]);
            RemoveDeserializersThatOnlyCallSuper(generatedCode);
            ShortenLongNamespaceNames(generatedCode);
            if (generatedCode.FindNamespaceByName(_configuration.ClientNamespaceName)?.Parent is CodeNamespace parentOfClientNS)
                AddNamespaceModuleImports(parentOfClientNS, generatedCode);
            var defaultConfiguration = new GenerationConfiguration();
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "microsoft_kiota_serialization_json.JsonSerializationWriterFactory"});
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "microsoft_kiota_serialization_json.JsonParseNodeFactory"});
            AddSerializationModulesImport(generatedCode,
                                        ["microsoft_kiota_abstractions.ApiClientBuilder",
                                            "microsoft_kiota_abstractions.SerializationWriterFactoryRegistry"],
                                        ["microsoft_kiota_abstractions.ParseNodeFactoryRegistry"]);
            AddQueryParameterMapperMethod(
                generatedCode
            );
            cancellationToken.ThrowIfCancellationRequested();
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: true
            );
            RequireBarrelsOfAutoloadedTypes(generatedCode, _configuration.ClientNamespaceName);
        }, cancellationToken);
    }
    private static void ShortenLongNamespaceNames(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace currentNamespace &&
            !string.IsNullOrEmpty(currentNamespace.Name) &&
            currentNamespace.Name.Split('.', StringSplitOptions.RemoveEmptyEntries) is string[] nameParts &&
            nameParts.Select(static x => x.ToSnakeCase()).Any(static x => x.Length > RubyPathSegmenter.MaxFileNameLength))
        {
            var newName = string.Join(".", nameParts
                                                .Select(static x => (originalName: x, snakeName: x.ToSnakeCase()))
                                                .Select(static x => x.snakeName.Length > RubyPathSegmenter.MaxFileNameLength ? x.originalName.GetNamespaceImportSymbol() : x.originalName));
            if (currentNamespace.Parent is CodeNamespace parentNamespace)
                parentNamespace.RenameChildElement(currentNamespace.Name, newName);

        }
        CrawlTree(currentElement, ShortenLongNamespaceNames);
    }
    /// <summary>
    /// A model sharing its name with a sibling namespace is suffixed so the two do not collide.
    /// References need no separate pass: CodeType.Name delegates to TypeDefinition.Name for a
    /// resolved, non-external type, so every reference already reports the new name. A pass that
    /// assigned to those names instead renamed the class again, once per reference it walked.
    /// </summary>
    private static void DisambiguateClassesWithNamespaceNames(CodeElement currentElement, string suffix)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Parent is CodeNamespace currentNamespace &&
            currentNamespace.FindChildByName<CodeNamespace>($"{currentNamespace.Name}.{currentClass.Name}") is not null)
        {
            currentNamespace.RemoveChildElement(currentClass);
            currentClass.Name = $"{currentClass.Name}{suffix}";
            currentNamespace.AddClass(currentClass);
        }
        CrawlTree(currentElement, x => DisambiguateClassesWithNamespaceNames(x, suffix));
    }
    // `\\.` matches a literal backslash and `(<letter>...)` is an ordinary group capturing the text
    // "<letter>", so the original pattern never matched and every nested model kept the dots from
    // its namespace, which are not legal in a Ruby constant
    [GeneratedRegex(@"\.(?<letter>\w)", RegexOptions.IgnoreCase | RegexOptions.Singleline, 500)]
    private static partial Regex CapitalizedFirstLetterAfterDot();
    private static void FlattenModelsNamespaces(CodeElement currentElement, CodeNamespace modelsNS)
    {
        // only classes and enums are moved up to the models namespace; without this guard any other
        // child, a nested namespace included, was still detached and renamed but never re-added,
        // which corrupted the prefix computed for every element visited afterwards
        if (currentElement is CodeClass or CodeEnum &&
            currentElement.Parent is CodeNamespace currentElementNamespace &&
            currentElementNamespace.IsChildOf(modelsNS))
        {
            var elementPrefix = CapitalizedFirstLetterAfterDot().Replace(currentElementNamespace.Name[(modelsNS.Name.Length + 1)..], x => x.Groups["letter"].Value.ToUpperInvariant());
            currentElementNamespace.RemoveChildElement(currentElement);
            currentElement.Name = $"{elementPrefix}{currentElement.Name.ToFirstCharacterUpperCase()}";
            if (currentElement is CodeClass currentClass)
                modelsNS.AddClass(currentClass);
            else if (currentElement is CodeEnum currentEnum)
                modelsNS.AddEnum(currentEnum);
        }
        CrawlTree(currentElement, x => FlattenModelsNamespaces(x, modelsNS));
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, types: currentMethod.Parameters
                                    .Select(x => x.Type)
                                    .Union(new[] { currentMethod.ReturnType })
                                    .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
        {"DateTimeOffset", ("DateTime", new CodeUsing {
                                        Name = "DateTime",
                                        Declaration = new CodeType {
                                            Name = "date",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("MicrosoftKiotaAbstractions::ISODuration", new CodeUsing {
                                        Name = "MicrosoftKiotaAbstractions::ISODuration",
                                        Declaration = new CodeType {
                                            Name = "microsoft_kiota_abstractions",
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", ("Date", new CodeUsing {
                                Name = "Date",
                                Declaration = new CodeType {
                                    Name = "date",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", ("Time", new CodeUsing {
                                Name = "Time",
                                Declaration = new CodeType {
                                    Name = "time",
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.PathParameters, CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.IsNullable = true;
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        }

        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, types: currentProperty.Type);

    }
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "microsoft_kiota_abstractions", "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "microsoft_kiota_abstractions", "HttpMethod", "RequestInformation", "RequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "microsoft_kiota_abstractions", "ResponseHandler"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "microsoft_kiota_abstractions", "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "microsoft_kiota_abstractions", "ParseNode"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            "microsoft_kiota_abstractions", "Parsable"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "microsoft_kiota_abstractions", "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(static y => y.IsOfKind(CodePropertyKind.AdditionalData)),
            "microsoft_kiota_abstractions", "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "microsoft_kiota_abstractions", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            "microsoft_kiota_abstractions", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
    };
    // a subclass with no fields of its own inherits the deserializer map unchanged
    private static void RemoveDeserializersThatOnlyCallSuper(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.StartBlock.Inherits is not null &&
            currentClass.OriginalComposedType is null &&
            !currentClass.Properties.Any(static x => x.IsOfKind(CodePropertyKind.Custom) && !x.ExistsInBaseType))
            currentClass.RemoveMethodByKinds(CodeMethodKind.Deserializer);
        CrawlTree(currentElement, RemoveDeserializersThatOnlyCallSuper);
    }
    private static void AddNamespaceModuleImports(CodeNamespace clientNamespaceParent, CodeElement current)
    {
        if (current is CodeClass currentClass)
        {
            var module = currentClass.GetImmediateParentOfType<CodeNamespace>();
            // loading its own barrel would register this file for autoload while it is still loading
            AddModules(clientNamespaceParent, RubyConventionService.IsAutoloaded(currentClass) ? module.Parent as CodeNamespace : module, (usingToAdd) =>
            {
                currentClass.AddUsing(usingToAdd);
            });
        }
        CrawlTree(current, c => AddNamespaceModuleImports(clientNamespaceParent, c));
    }
    private static void AddModules(CodeNamespace clientNamespaceParent, CodeNamespace? module, Action<CodeUsing> callback)
    {
        var definition = module;
        while (definition != clientNamespaceParent && !string.IsNullOrEmpty(definition?.Name))
        {
            callback(GetNamespaceUsing(definition));
            definition = definition.Parent as CodeNamespace;
        }
    }
    private static CodeUsing GetNamespaceUsing(CodeNamespace codeNamespace) => new()
    {
        Name = codeNamespace.Name,
        Declaration = new CodeType
        {
            IsExternal = false,
            Name = codeNamespace.Name,
            TypeDefinition = codeNamespace,
        }
    };
    // an autoloaded file is only ever loaded through its barrel, so other files require the barrel
    private static void RequireBarrelsOfAutoloadedTypes(CodeElement currentElement, string clientNamespaceName)
    {
        if (currentElement is CodeClass { Parent: CodeNamespace currentNamespace } currentClass)
        {
            // a namespace with nothing to autoload gets no barrel file, so nothing may require one;
            // the client's root file always exists and only the client requires it, for eager_load!
            var isClient = currentClass.Methods.Any(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
            currentClass.StartBlock.RemoveUsings(currentClass.Usings
                                        .Where(x => !x.IsExternal && x.Declaration?.TypeDefinition is CodeNamespace ns && !RubyConventionService.HasAutoloadedMembers(ns) &&
                                                    !(isClient && ns.Name.Equals(clientNamespaceName, StringComparison.OrdinalIgnoreCase)))
                                        .ToArray());
            var typeUsings = currentClass.Usings
                                        .Where(static x => !x.IsExternal && x.Declaration?.TypeDefinition is CodeElement definition && RubyConventionService.IsAutoloaded(definition))
                                        .ToArray();
            if (typeUsings.Length != 0)
            {
                currentClass.StartBlock.RemoveUsings(typeUsings);
                var isAutoloaded = RubyConventionService.IsAutoloaded(currentClass);
                var barrels = typeUsings.Select(static x => x.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>())
                                        .Where(x => !(isAutoloaded && x == currentNamespace))
                                        .Distinct()
                                        .Where(x => !currentClass.Usings.Any(y => y.Declaration?.TypeDefinition == x))
                                        .Select(GetNamespaceUsing)
                                        .ToArray();
                currentClass.AddUsing(barrels);
            }
        }
        CrawlTree(currentElement, x => RequireBarrelsOfAutoloadedTypes(x, clientNamespaceName));
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements
            .Where(static x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(static x => x.Name = "MicrosoftKiotaAbstractions::AdditionalDataHolder");
    }
}
