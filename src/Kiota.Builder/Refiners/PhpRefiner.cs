using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class PhpRefiner : CommonLanguageRefiner
{
    private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
    public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }


    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Imports should be done before adding getters and setters since AddGetterAndSetterMethods can remove properties from classes when backing store is enabled
            ReplaceReservedNames(generatedCode, new PhpReservedNamesProvider(), reservedWord => $"Escaped{reservedWord.ToFirstCharacterUpperCase()}");
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiException",
                "Microsoft\\Kiota\\Abstractions"
            );
            ReplaceReservedExceptionPropertyNames(generatedCode, new PhpExceptionsReservedNamesProvider(), static x => $"escaped{x.ToFirstCharacterUpperCase()}");
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                false);
            cancellationToken.ThrowIfCancellationRequested();
            CorrectParameterType(generatedCode);
            MakeModelPropertiesNullable(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, false, "ById");
            cancellationToken.ThrowIfCancellationRequested();
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: true
            );
            var defaultConfiguration = new GenerationConfiguration();
            ReplaceDefaultSerializationModules(generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "Microsoft\\Kiota\\Serialization\\Json\\JsonSerializationWriterFactory",
                    "Microsoft\\Kiota\\Serialization\\Text\\TextSerializationWriterFactory"}
            );
            ReplaceDefaultDeserializationModules(generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "Microsoft\\Kiota\\Serialization\\Json\\JsonParseNodeFactory",
                    "Microsoft\\Kiota\\Serialization\\Text\\TextParseNodeFactory"}
            );
            cancellationToken.ThrowIfCancellationRequested();
            AddSerializationModulesImport(generatedCode, new[] { "Microsoft\\Kiota\\Abstractions\\ApiClientBuilder" }, null, '\\');
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            AddInnerClasses(generatedCode,
                true,
                string.Empty,
                true);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore
                },
                static s => s.ToCamelCase(new[] { '_' }),
                _configuration.UsesBackingStore,
                true,
                "get",
                "set");
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "StreamInterface", "Psr\\Http\\Message", true, _configuration.UsesBackingStore);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton::getInstance()->createBackingStore()");
            CorrectBackingStoreSetterParam(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AliasUsingWithSameSymbol(generatedCode);
            RemoveHandlerFromRequestBuilder(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "DateOnly",("Date", new CodeUsing
            {
                Name = "Date",
                Declaration = new CodeType
                {
                    Name = "Microsoft\\Kiota\\Abstractions\\Types",
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
                    Name = "Microsoft\\Kiota\\Abstractions\\Types",
                    IsExternal = true,
                },
            })
        },
        {
            "TimeSpan", ("DateInterval", new CodeUsing
            {
                Name = "DateInterval",
                Declaration = new CodeType
                {
                    Name = "",
                    IsExternal = true
                }
            })
        },
        {
            "DateTimeOffset", ("DateTime", new CodeUsing
            {
                Name = "DateTime",
                Declaration = new CodeType
                {
                    Name = "",
                    IsExternal = true
                }
            })
        }
    };
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "Microsoft\\Kiota\\Abstractions", "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "Microsoft\\Kiota\\Abstractions", "HttpMethod", "RequestInformation"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "Microsoft\\Kiota\\Abstractions", "ResponseHandler"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(static y => y.IsOfKind(CodePropertyKind.AdditionalData)),
            "Microsoft\\Kiota\\Abstractions\\Serialization", "AdditionalDataHolder"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "Microsoft\\Kiota\\Abstractions\\Serialization", "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "Microsoft\\Kiota\\Abstractions\\Serialization", "ParseNode"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "Microsoft\\Kiota\\Abstractions\\Serialization", "Parsable", "ParsableFactory"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            "Microsoft\\Kiota\\Abstractions\\Serialization", "Parsable"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "Microsoft\\Kiota\\Abstractions\\Store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            "Microsoft\\Kiota\\Abstractions\\Store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor), "Http\\Promise", "Promise", "RejectedPromise"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor), "", "Exception"),
        new (x => x is CodeEnum, "Microsoft\\Kiota\\Abstractions\\", "Enum"),
        new(static x => x is CodeProperty {Type.Name: {}} property && property.Type.Name.Equals("DateTime", StringComparison.OrdinalIgnoreCase), "", "\\DateTime"),
        new(static x => x is CodeProperty {Type.Name: {}} property && property.Type.Name.Equals("DateTimeOffset", StringComparison.OrdinalIgnoreCase), "", "\\DateTime"),
        new(x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor), "Microsoft\\Kiota\\Abstractions", "ApiClientBuilder"),
        new(x => x is CodeProperty property && property.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(property.SerializationName), "Microsoft\\Kiota\\Abstractions", "QueryParameter"),
        new(x => x is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestConfiguration), "Microsoft\\Kiota\\Abstractions", "RequestOption"),
        new (static x => x is CodeClass { OriginalComposedType: CodeIntersectionType intersectionType } && intersectionType.Types.Any(static y => !y.IsExternal) && intersectionType.DiscriminatorInformation.HasBasicDiscriminatorInformation,
            "Microsoft\\Kiota\\Serialization", "ParseNodeHelper"),
    };
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            currentProperty.Type.Name = "RequestAdapter";
            currentProperty.Type.IsNullable = false;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..];
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "array";
            currentProperty.Type.IsNullable = false;
            currentProperty.DefaultValue = "[]";
            currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = false;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "array";
            currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "[]";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.RequestBuilder))
        {
            currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
        }
        else if (currentProperty.Type.Name?.Equals("DateTimeOffset", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            currentProperty.Type.Name = "DateTime";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.Options, CodePropertyKind.Headers))
        {
            currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
            currentProperty.Type.Name = "array";
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }

    private static void CorrectMethodType(CodeMethod method)
    {
        if (method.IsOfKind(CodeMethodKind.Deserializer))
        {
            method.ReturnType.Name = "array";
        }
        CorrectCoreTypes(method.Parent as CodeClass, DateTypesReplacements, method.Parameters
            .Select(static x => x.Type)
            .Union(new[] { method.ReturnType })
            .ToArray());
    }
    private static void CorrectParameterType(CodeElement codeElement)
    {
        if (codeElement is CodeMethod currentMethod)
        {
            currentMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.ParseNode, CodeParameterKind.PathParameters)).ToList().ForEach(static x =>
            {
                if (x.IsOfKind(CodeParameterKind.ParseNode))
                    x.Type.Name = "ParseNode";
                else
                    x.Documentation.Description += " or a String representing the raw URL.";
            });
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.BackingStore)
                                                && currentMethod.IsOfKind(CodeMethodKind.ClientConstructor)).ToList().ForEach(static x =>
            {
                x.Type.Name = "BackingStoreFactory";
                x.DefaultValue = "null";
            });
        }
        CrawlTree(codeElement, CorrectParameterType);
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }

    private static void AliasUsingWithSameSymbol(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.StartBlock != null && currentClass.StartBlock.Usings.Any(x => !x.IsExternal))
        {
            var duplicatedSymbolsUsings = currentClass.StartBlock.Usings
                .Distinct(usingComparer)
                .Where(static x => !string.IsNullOrEmpty(x.Declaration?.Name) && x.Declaration.TypeDefinition != null)
                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .Union(currentClass.StartBlock
                    .Usings
                    .Where(x => !x.IsExternal)
                    .Where(x => x.Declaration!
                        .Name
                        .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
            foreach (var usingElement in duplicatedSymbolsUsings)
            {
                var replacement = string.Join("\\", usingElement.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>().Name
                    .Split(new[] { '\\', '.' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.ToFirstCharacterUpperCase())
                    .ToArray());
                usingElement.Alias = $"{(string.IsNullOrEmpty(replacement) ? string.Empty : $"\\{replacement}")}\\{usingElement.Declaration!.TypeDefinition!.Name.ToFirstCharacterUpperCase()}";
                usingElement.Declaration.Name = usingElement.Alias;
            }
        }
        CrawlTree(currentElement, AliasUsingWithSameSymbol);
    }

    private static void CorrectBackingStoreSetterParam(CodeElement codeElement)
    {
        if (codeElement is CodeMethod method && method.Kind == CodeMethodKind.Setter && method.AccessedProperty?.Kind == CodePropertyKind.BackingStore)
            method.Parameters.ToList().ForEach(param => param.Optional = false);
        CrawlTree(codeElement, CorrectBackingStoreSetterParam);
    }
}

