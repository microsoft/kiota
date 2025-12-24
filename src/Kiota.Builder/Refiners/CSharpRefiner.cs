using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public CSharpRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddPrimaryErrorMessage(generatedCode,
                "Message",
                () => new CodeType { Name = "string", IsNullable = false, IsExternal = true },
                true
            );
            DeduplicateErrorMappings(generatedCode);
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                });

            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                },
                new CodeType
                {
                    Name = "DefaultQueryParameters",
                    IsExternal = true,
                },
                !_configuration.ExcludeBackwardCompatible,//TODO remove the condition for v2
                !_configuration.ExcludeBackwardCompatible);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s,
                true,
                SerializationNamespaceName,
                "IComposedTypeWrapper"
            );
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddAsyncSuffix(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddParsableImplementsForModelClasses(generatedCode, "IParsable");
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            /* Exclude the following as their names will be capitalized making the change unnecessary in this case sensitive language
                * code classes, class declarations, property names, using declarations, namespace names
                * Exclude CodeMethod as the return type will also be capitalized (excluding the CodeType is not enough since this is evaluated at the code method level)
            */
            ReplaceReservedNames(
                generatedCode,
                new CSharpReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}",
                new HashSet<Type> { typeof(CodeClass), typeof(ClassDeclaration), typeof(CodeProperty), typeof(CodeUsing), typeof(CodeNamespace), typeof(CodeMethod), typeof(CodeEnum), typeof(CodeEnumOption) }
            );
            ReplaceReservedNames(
                generatedCode,
                new CSharpReservedClassNamesProvider(),
                x => $"{x.ToFirstCharacterUpperCase()}Escaped"
            );
            ReplaceReservedExceptionPropertyNames(
                generatedCode,
                new CSharpExceptionsReservedNamesProvider(),
                static x => $"{x.ToFirstCharacterUpperCase()}Escaped"
            );
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedModelTypes(generatedCode, new CSharpReservedTypesProvider(), x => $"{x}Object");
            ReplaceReservedNamespaceTypeNames(generatedCode, new CSharpReservedTypesProvider(), static x => $"{x}Namespace");
            ReplacePropertyNames(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                },
                static s => s.ToPascalCase(UnderscoreArray));
            DisambiguatePropertiesWithClassNames(generatedCode);
            // Correct the core types after reserved names for types/properties are done to avoid collision of types e.g. renaming custom model called `DateOnly` to `Date`
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, correctIndexer: CorrectIndexerType);
            cancellationToken.ThrowIfCancellationRequested();
            AddSerializationModulesImport(generatedCode);
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiException",
                AbstractionsNamespaceName
            );
            AddConstructorsForDefaultValues(generatedCode, false);
            AddConstructorsForErrorClasses(generatedCode);
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "IParseNode"
            );
            SetTypeAccessModifiers(generatedCode);
        }, cancellationToken);
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
    protected static void MakeEnumPropertiesNullable(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
            currentClass.Properties
                        .Where(x => x.Type is CodeType propType && propType.TypeDefinition is CodeEnum)
                        .ToList()
                        .ForEach(x => x.Type.IsNullable = true);
        CrawlTree(currentElement, MakeEnumPropertiesNullable);
    }
    private const string AbstractionsNamespaceName = "Microsoft.Kiota.Abstractions";
    private const string SerializationNamespaceName = $"{AbstractionsNamespaceName}.Serialization";
    private const string StoreNamespaceName = $"{AbstractionsNamespaceName}.Store";
    private const string ExtensionsNamespaceName = $"{AbstractionsNamespaceName}.Extensions";

    protected static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "IRequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "Method", "RequestInformation", "IRequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            SerializationNamespaceName, "ISerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            SerializationNamespaceName, "IParseNode"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor),
            ExtensionsNamespaceName, "Dictionary"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            SerializationNamespaceName, "IParsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            SerializationNamespaceName, "IAdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            SerializationNamespaceName, "IParsable"),
        new (static x => x is CodeClass || x is CodeEnum,
            "System", "String"),
        new (static x => x is CodeClass,
            "System.Collections.Generic", "List", "Dictionary"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
            "System.IO", "Stream"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "System.Threading", "CancellationToken"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "System.Threading.Tasks", "Task"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
            ExtensionsNamespaceName, "Enumerable"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            StoreNamespaceName,  "IBackingStoreFactory", "IBackingStoreFactorySingleton"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            StoreNamespaceName,  "IBackingStore", "IBackedModel", "BackingStoreFactorySingleton" ),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(prop.SerializationName),
            AbstractionsNamespaceName, "QueryParameterAttribute"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal),
            SerializationNamespaceName, "ParseNodeHelper"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase),
            SerializationNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeMethod @method && @method.IsOfKind(CodeMethodKind.RequestExecutor) && (method.ReturnType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase) ||
                                                                                                        method.Parameters.Any(x => x.Kind is CodeParameterKind.RequestBody && x.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))),
            SerializationNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeEnum prop && prop.Options.Any(x => x.IsNameEscaped),
            "System.Runtime.Serialization", "EnumMemberAttribute"),
        new (static x => x is IDeprecableElement element && element.Deprecation is not null && element.Deprecation.IsDeprecated,
            "System", "ObsoleteAttribute"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
    };
    private const string MultipartBodyClassName = "MultipartBody";
    protected static void CapitalizeNamespacesFirstLetters(CodeElement current)
    {
        if (current is CodeNamespace currentNamespace)
            currentNamespace.Name = currentNamespace.Name.Split('.').Select(static x => x.ToFirstCharacterUpperCase()).Aggregate(static (x, y) => $"{x}.{y}");
        CrawlTree(current, CapitalizeNamespacesFirstLetters);
    }
    protected static void AddAsyncSuffix(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
            currentMethod.Name += "Async";
        CrawlTree(currentElement, AddAsyncSuffix);
    }
    protected static void CorrectPropertyType(CodeProperty currentProperty)
    {
        ArgumentNullException.ThrowIfNull(currentProperty);
        if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "new List<IRequestOption>()";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, true, currentProperty.Type);
    }
    protected static void CorrectMethodType(CodeMethod currentMethod)
    {
        ArgumentNullException.ThrowIfNull(currentMethod);
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, true, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    protected static void CorrectIndexerType(CodeIndexer currentIndexer)
    {
        ArgumentNullException.ThrowIfNull(currentIndexer);
        CorrectCoreTypes(currentIndexer.Parent as CodeClass, DateTypesReplacements, true, currentIndexer.IndexParameter.Type);
    }

    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "DateOnly",("Date", new CodeUsing
                {
                    Name = "Date",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true,
                    },
                })
        },
        {
            "TimeOnly",("Time", new CodeUsing
                {
                    Name = "Time",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true,
                    },
                })
        },
    };

    private void SetTypeAccessModifiers(CodeElement currentElement)
    {
        if (currentElement is IAccessibleElement accessibleElement and (CodeEnum or CodeClass))
        {
            accessibleElement.Access = _configuration.TypeAccessModifier;
        }

        CrawlTree(currentElement, SetTypeAccessModifiers);
    }

    private static void AddConstructorsForErrorClasses(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass && codeClass.IsErrorDefinition)
        {
            // Add parameterless constructor if not already present
            if (!codeClass.Methods.Any(static x => x.IsOfKind(CodeMethodKind.Constructor) && !x.Parameters.Any()))
            {
                var parameterlessConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} and sets the default values.");
                codeClass.AddMethod(parameterlessConstructor);
            }

            var messageParameter = CreateErrorMessageParameter("string");
            // Add message constructor if not already present
            if (!codeClass.Methods.Any(static x => x.IsOfKind(CodeMethodKind.Constructor) && x.Parameters.Any(static p => p.IsOfKind(CodeParameterKind.ErrorMessage))))
            {
                var messageConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} with the specified error message.");
                messageConstructor.AddParameter(messageParameter);
                codeClass.AddMethod(messageConstructor);
            }

            var method = TryAddErrorMessageFactoryMethod(
                    codeClass,
                    methodName: "CreateFromDiscriminatorValueWithMessage",
                    messageParameter: messageParameter,
                    parseNodeTypeName: "IParseNode");
        }
        CrawlTree(currentElement, AddConstructorsForErrorClasses);
    }
}
