﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Dart;

namespace Kiota.Builder.Refiners;
public class DartRefinerFromScratch : CommonLanguageRefiner, ILanguageRefiner
{
    private const string MultipartBodyClassName = "MultipartBody";
    private const string AbstractionsNamespaceName = "kiota_abstractions/kiota_abstractions";
    private const string SerializationNamespaceName = "kiota_serialization";

    protected static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "Method", "RequestInformation", "RequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            AbstractionsNamespaceName, "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            AbstractionsNamespaceName, "ParseNode"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            AbstractionsNamespaceName, "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            AbstractionsNamespaceName, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsNamespaceName, "Parsable"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(prop.SerializationName),
            AbstractionsNamespaceName, "QueryParameterAttribute"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal),
            AbstractionsNamespaceName, "ParseNodeHelper"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase),
            SerializationNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
        new (static x => x is CodeProperty prop && prop.Type.Name.EqualsIgnoreCase("Guid"),
            "uuid/uuid", "Uuid", "UuidValue"),
    };


    public DartRefinerFromScratch(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();
            CorrectCommonNames(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterLowerCase(),
                GenerationLanguage.Dart);
            AddQueryParameterExtractorMethod(generatedCode);
            // This adds the BaseRequestBuilder class as a superclass
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true,
                    }
                }, addCurrentTypeAsGenericTypeParameter: true);
            RemoveRequestConfigurationClasses(generatedCode,
                        new CodeUsing
                        {
                            Name = "RequestConfiguration",
                            Declaration = new CodeType
                            {
                                Name = AbstractionsNamespaceName,
                                IsExternal = true
                            }
                        }, new CodeType
                        {
                            Name = "DefaultQueryParameters",
                            IsExternal = true,
                        });
            var reservedNamesProvider = new DartReservedNamesProvider();
            CorrectNames(generatedCode, s =>
            {
                if (s.Contains('_', StringComparison.OrdinalIgnoreCase) &&
                     s.ToPascalCase(UnderscoreArray) is string refinedName &&
                    !reservedNamesProvider.ReservedNames.Contains(s) &&
                    !reservedNamesProvider.ReservedNames.Contains(refinedName))
                    return refinedName;
                else
                    return s;
            });

            MoveQueryParameterClass(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true, codeTypeFilter);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();
            AddAsyncSuffix(generatedCode);
            AddDiscriminatorMappingsUsingsToParentClasses(generatedCode, "ParseNode", addUsings: true, includeParentNamespace: true);

            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json/{SerializationNamespaceName}_json.JsonSerializationWriterFactory",
                    $"{SerializationNamespaceName}_text/{SerializationNamespaceName}_text.TextSerializationWriterFactory",
                    $"{SerializationNamespaceName}_form/{SerializationNamespaceName}_form.FormSerializationWriterFactory",
                    // $"{SerializationNamespaceName}_multi/{SerializationNamespaceName}_multi.MultipartSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json/{SerializationNamespaceName}_json.JsonParseNodeFactory",
                    $"{SerializationNamespaceName}_form/{SerializationNamespaceName}_form.FormParseNodeFactory",
                    $"{SerializationNamespaceName}_text/{SerializationNamespaceName}_text.TextParseNodeFactory"
                }
            );
            AddSerializationModulesImport(generatedCode,
                [$"{AbstractionsNamespaceName}.ApiClientBuilder",
                 $"{AbstractionsNamespaceName}.SerializationWriterFactoryRegistry"],
                [$"{AbstractionsNamespaceName}.ParseNodeFactoryRegistry"]);
            cancellationToken.ThrowIfCancellationRequested();

            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiException",
                    AbstractionsNamespaceName
            );
            AddPrimaryErrorMessage(generatedCode,
                "message",
                () => new CodeType { Name = "string", IsNullable = true, IsExternal = false },
                true
            );
            DeduplicateErrorMappings(generatedCode);
            RemoveCancellationParameter(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            DisambiguatePropertiesWithClassNames(generatedCode);
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlBuilder);
            AddCloneMethodToRequestBuilders(generatedCode);
            escapeStringValues(generatedCode);
        }, cancellationToken);
    }

    /// <summary> 
    /// Corrects common names so they can be used with Dart.
    /// This normally comes down to changing the first character to lower case.
    /// <example><code>GetFieldDeserializers</code> is corrected to <code>getFieldDeserializers</code>
    /// </summary>
    private static void CorrectCommonNames(CodeElement currentElement)
    {
        if (currentElement is CodeMethod m &&
            currentElement.Parent is CodeClass parentClass)
        {
            parentClass.RenameChildElement(m.Name, m.Name.ToFirstCharacterLowerCase());
        }
        else if (currentElement is CodeIndexer i)
        {
            i.IndexParameter.Name = i.IndexParameter.Name.ToFirstCharacterLowerCase();
        }
        CrawlTree(currentElement, element => CorrectCommonNames(element));
    }

    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "Map<String, void Function(ParseNode)>";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlConstructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                .Where(x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
    }

    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        ArgumentNullException.ThrowIfNull(currentProperty);

        if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "List<RequestOption>()";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = $"{currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        else if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            currentProperty.Type.Name = "RequestAdapter";
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
        {
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            currentProperty.Name = currentProperty.Name.ToFirstCharacterLowerCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameter))
        {
            currentProperty.DefaultValue = $"{currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Map<String, Object?>";
            currentProperty.DefaultValue = "{}";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "Map<String, dynamic>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        }
        if (currentProperty.Type.Name.Equals("Guid", StringComparison.OrdinalIgnoreCase))
            currentProperty.Type.Name = "UuidValue";

        currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
    }

    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    public static IEnumerable<CodeTypeBase> codeTypeFilter(IEnumerable<CodeTypeBase> usingsToAdd)
    {
        var genericParameterTypes = usingsToAdd.OfType<CodeType>().Where(
            static codeType => codeType.Parent is CodeParameter parameter
            && parameter.IsOfKind(CodeParameterKind.RequestConfiguration)).Select(x => x.GenericTypeParameterValues.First());

        return usingsToAdd.Union(genericParameterTypes);
    }
    protected static void AddAsyncSuffix(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
            currentMethod.Name += "Async";
        CrawlTree(currentElement, AddAsyncSuffix);
    }
    private void AddQueryParameterExtractorMethod(CodeElement currentElement, string methodName = "getQueryParameters")
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.QueryParameters))
        {
            currentClass.StartBlock.AddImplements(new CodeType
            {
                IsExternal = true,
                Name = "AbstractQueryParameters"
            });
            currentClass.AddMethod(new CodeMethod
            {
                Name = methodName,
                Access = AccessModifier.Public,
                ReturnType = new CodeType
                {
                    Name = "Map<String, dynamic>",
                    IsNullable = false,
                },
                IsAsync = false,
                IsStatic = false,
                Kind = CodeMethodKind.QueryParametersMapper,
                Documentation = new()
                {
                    DescriptionTemplate = "Extracts the query parameters into a map for the URI template parsing.",
                },
            });
            currentClass.AddUsing(new CodeUsing
            {
                Name = "AbstractQueryParameters",
                Declaration = new CodeType { Name = AbstractionsNamespaceName, IsExternal = true },
            });
        }
        CrawlTree(currentElement, x => AddQueryParameterExtractorMethod(x, methodName));
    }

    private void MoveQueryParameterClass(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var parentNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
            var nestedClasses = currentClass.InnerClasses.Where(x => x.IsOfKind(CodeClassKind.QueryParameters));
            foreach (CodeClass nestedClass in nestedClasses)
            {
                parentNamespace.AddClass(nestedClass);
                currentClass.RemoveChildElementByName(nestedClass.Name);
            }
        }
        CrawlTree(currentElement, x => MoveQueryParameterClass(x));
    }

    protected static void DisambiguatePropertiesWithClassNames(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            var sameNameProperty = currentClass.Properties
                                            .FirstOrDefault(x => x.Name.Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase));
            if (sameNameProperty != null)
            {
                currentClass.RemoveChildElement(sameNameProperty);
                if (string.IsNullOrEmpty(sameNameProperty.SerializationName))
                    sameNameProperty.SerializationName = sameNameProperty.Name;
                sameNameProperty.Name = $"{sameNameProperty.Name}Prop";
                currentClass.AddProperty(sameNameProperty);
            }
        }
        CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
    }
    private void AddCloneMethodToRequestBuilders(CodeElement currentElement, string methodName = "clone")
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            currentClass.AddMethod(new CodeMethod
            {
                Name = methodName,
                Access = AccessModifier.Public,
                ReturnType = new CodeType
                {
                    Name = currentClass.Name,
                    IsNullable = false,
                },
                IsAsync = false,
                IsStatic = false,

                Kind = CodeMethodKind.Custom,
                Documentation = new()
                {
                    DescriptionTemplate = "Clones the requestbuilder.",
                },
            });
        }
        CrawlTree(currentElement, x => AddCloneMethodToRequestBuilders(x, methodName));
    }

    private void escapeStringValues(CodeElement currentElement)
    {
        if (currentElement is CodeProperty property &&
            property.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            if (property.DefaultValue.Contains('$', StringComparison.Ordinal))
            {
                property.DefaultValue = property.DefaultValue.Replace("$", "\\$", StringComparison.Ordinal);
            }
        }
        else if (currentElement is CodeProperty prop)
        {
            if (!String.IsNullOrEmpty(prop.SerializationName) && prop.SerializationName.Contains('$', StringComparison.Ordinal))
            {
                prop.SerializationName = prop.SerializationName.Replace("$", "\\$", StringComparison.Ordinal);
            }
        }
        CrawlTree(currentElement, escapeStringValues);
    }
}
