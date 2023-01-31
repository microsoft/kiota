﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class RubyRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public RubyRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode, false, "_by_id");
            var classesToDisambiguate = new HashSet<CodeClass>();
            var suffix = "Model";
            DisambiguateClassesWithNamespaceNames(generatedCode, classesToDisambiguate, suffix);
            UpdateReferencesToDisambiguatedClasses(generatedCode, classesToDisambiguate, suffix);
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, true);
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore
            );
            cancellationToken.ThrowIfCancellationRequested();
            AddParsableImplementsForModelClasses(generatedCode, "MicrosoftKiotaAbstractions::Parsable");
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore,
                },
                _configuration.UsesBackingStore,
                true,
                string.Empty,
                string.Empty,
                string.Empty);
            AddConstructorsForDefaultValues(
                generatedCode,
                true,
                false,
                new[] { CodeClassKind.RequestConfiguration });
            ReplaceReservedNames(generatedCode, new RubyReservedNamesProvider(), x => $"{x}_escaped");
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
                                        new[] { "microsoft_kiota_abstractions.ApiClientBuilder",
                                                "microsoft_kiota_abstractions.SerializationWriterFactoryRegistry" },
                                        new[] { "microsoft_kiota_abstractions.ParseNodeFactoryRegistry" });
            AddQueryParameterMapperMethod(
                generatedCode
            );
            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiError",
                    "MicrosoftKiotaAbstractions",
                    true
            );
            cancellationToken.ThrowIfCancellationRequested();
            RemoveDiscriminatorMappingsThatDependOnSubNameSpace(generatedCode);
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: true
            );
            RemoveHandlerFromRequestBuilder(generatedCode);
        }, cancellationToken);
    }
    private static void DisambiguateClassesWithNamespaceNames(CodeElement currentElement, HashSet<CodeClass> classesToUpdate, string suffix)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Parent is CodeNamespace currentNamespace &&
            currentNamespace.FindChildByName<CodeNamespace>($"{currentNamespace.Name}.{currentClass.Name}") is not null)
        {
            currentNamespace.RemoveChildElement(currentClass);
            currentClass.Name = $"{currentClass.Name}{suffix}";
            currentNamespace.AddClass(currentClass);
            classesToUpdate.Add(currentClass);
        }
        CrawlTree(currentElement, x => DisambiguateClassesWithNamespaceNames(x, classesToUpdate, suffix));
    }
    private static void UpdateReferencesToDisambiguatedClasses(CodeElement currentElement, HashSet<CodeClass> classesToUpdate, string suffix)
    {
        if (!classesToUpdate.Any()) return;
        if (currentElement is CodeProperty currentProperty &&
            currentProperty.Type is CodeType propertyType &&
            propertyType.TypeDefinition is CodeClass propertyTypeClass &&
            classesToUpdate.Contains(propertyTypeClass))
            propertyType.Name = $"{propertyType.Name}{suffix}";
        else if (currentElement is CodeMethod currentMethod)
        {
            if (currentMethod.ReturnType is CodeType returnType &&
                returnType.TypeDefinition is CodeClass returnTypeClass &&
                classesToUpdate.Contains(returnTypeClass))
                returnType.Name = $"{returnType.Name}{suffix}";
            currentMethod.Parameters.Where(x => x.Type is CodeType parameterType &&
                                                parameterType.TypeDefinition is CodeClass parameterTypeClass &&
                                                classesToUpdate.Contains(parameterTypeClass))
                                    .ToList()
                                    .ForEach(x => x.Type.Name = $"{x.Type.Name}{suffix}");
        }
        else if (currentElement is CodeClass currentClass)
        {
            if (currentClass.StartBlock.Inherits?.TypeDefinition is CodeClass parentClass &&
                    classesToUpdate.Contains(parentClass))
                currentClass.StartBlock.Inherits.Name = $"{currentClass.StartBlock.Inherits.Name}{suffix}";
            currentClass.DiscriminatorInformation
                        .DiscriminatorMappings
                        .Select(static x => x.Value)
                        .OfType<CodeType>()
                        .Where(x => x.TypeDefinition is CodeClass typeClass && classesToUpdate.Contains(typeClass))
                        .ToList()
                        .ForEach(x => x.Name = $"{x.Name}{suffix}");
            currentClass.Usings
                        .Where(static x => !x.IsExternal)
                        .Select(static x => x.Declaration)
                        .OfType<CodeType>()
                        .Where(x => x.TypeDefinition is CodeClass typeClass && classesToUpdate.Contains(typeClass))
                        .ToList()
                        .ForEach(x => x.Name = $"{x.Name}{suffix}");
        }
        CrawlTree(currentElement, x => UpdateReferencesToDisambiguatedClasses(x, classesToUpdate, suffix));
    }
    private static void RemoveDiscriminatorMappingsThatDependOnSubNameSpace(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation &&
            currentClass.Parent is CodeNamespace classNameSpace)
            currentClass.DiscriminatorInformation.RemoveDiscriminatorMapping(currentClass.DiscriminatorInformation
                                                .DiscriminatorMappings
                                                .Where(x => x.Value is CodeType mapping &&
                                                            mapping.TypeDefinition is CodeClass mappingClass &&
                                                            mappingClass.Parent is CodeNamespace mappingClassNamespace &&
                                                            mappingClassNamespace.IsChildOf(classNameSpace))
                                                .Select(static x => x.Key)
                                                .ToArray());
        CrawlTree(currentElement, RemoveDiscriminatorMappingsThatDependOnSubNameSpace);
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
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
                currentProperty.DefaultValue = "Hash.new";
        }

        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);

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
    private static void AddInheritedAndMethodTypesImports(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)
            && currentClass.StartBlock.Inherits != null)
        {
            currentClass.AddUsing(new CodeUsing { Name = currentClass.StartBlock.Inherits.Name, Declaration = currentClass.StartBlock.Inherits });
        }
        CrawlTree(currentElement, AddInheritedAndMethodTypesImports);
    }
    private static void AddNamespaceModuleImports(CodeNamespace clientNamespaceParent, CodeElement current)
    {
        if (current is CodeClass currentClass)
        {
            var module = currentClass.GetImmediateParentOfType<CodeNamespace>();
            AddModules(clientNamespaceParent, module, (usingToAdd) =>
            {
                currentClass.AddUsing(usingToAdd);
            });
        }
        CrawlTree(current, c => AddNamespaceModuleImports(clientNamespaceParent, c));
    }
    private static void AddModules(CodeNamespace clientNamespaceParent, CodeNamespace module, Action<CodeUsing> callback)
    {
        var definition = module;
        while (definition != clientNamespaceParent && !string.IsNullOrEmpty(definition?.Name))
        {
            callback(new CodeUsing
            {
                Name = definition.Name,
                Declaration = new CodeType
                {
                    IsExternal = false,
                    Name = definition.Name,
                    TypeDefinition = definition,
                }
            });
            definition = definition.Parent as CodeNamespace;
        }
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements
            .Where(static x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(static x => x.Name = "MicrosoftKiotaAbstractions::AdditionalDataHolder");
    }
}
