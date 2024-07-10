using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class CliRefiner : CSharpRefiner, ILanguageRefiner
{
    private static readonly CodePropertyKind[] UnusedPropKinds = new[] { CodePropertyKind.RequestAdapter };
    private static readonly CodeParameterKind[] UnusedParamKinds = new[] { CodeParameterKind.RequestAdapter };
    private static readonly CodeMethodKind[] ConstructorKinds = new[] { CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor };
    public CliRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseCliRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = "Microsoft.Kiota.Cli.Commons",
                        IsExternal = true
                    }
                });
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlBuilder);
            RemoveBackwardCompatibleIndexers(generatedCode);
            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = "Microsoft.Kiota.Abstractions",
                        IsExternal = true
                    }
                },
                new CodeType
                {
                    Name = "DefaultQueryParameters",
                    IsExternal = true,
                });
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddDefaultImports(generatedCode, additionalUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            cancellationToken.ThrowIfCancellationRequested();
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s
            );
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            cancellationToken.ThrowIfCancellationRequested();
            AddParsableImplementsForModelClasses(generatedCode, "IParsable");
            CapitalizeNamespacesFirstLetters(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
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
                new CliReservedClassNamesProvider(),
                x => $"{x.ToFirstCharacterUpperCase()}Escaped"
            );
            // Replace the reserved types
            ReplaceReservedModelTypes(generatedCode, new CSharpReservedTypesProvider(), x => $"{x}Object");
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNamespaceTypeNames(generatedCode, new CSharpReservedTypesProvider(), static x => $"{x}Namespace");
            ReplacePropertyNames(generatedCode,
                [
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                ],
                static s => s.ToPascalCase(UnderscoreArray));
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiException",
                "Microsoft.Kiota.Abstractions"
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "IParseNode"
            );
            cancellationToken.ThrowIfCancellationRequested();
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
            AddSerializationModulesImport(generatedCode);
            RenameDuplicateIndexerNavProperties(generatedCode);
            // Must be called after the more specific RenameDuplicateIndexerNavProperties
            RenameMatchingSubsequentNavCommands(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            CreateCommandBuilders(generatedCode);
        }, cancellationToken);
    }
    private static void RemoveBackwardCompatibleIndexers(CodeElement currentElement)
    {
        //TODO remove for v2
        if (currentElement is CodeClass
            {
                Kind: CodeClassKind.RequestBuilder, Indexer: not null
            } currentClass &&
            currentClass.GetChildElements(true)
                .OfType<CodeIndexer>()
                .FirstOrDefault(static i => i.IsLegacyIndexer) is { } backwardCompatibleIndexer)
        {
            currentClass.RemoveChildElement(backwardCompatibleIndexer);
        }

        CrawlTree(currentElement, RemoveBackwardCompatibleIndexers);
    }

    private static void RenameDuplicateIndexerNavProperties(CodeElement currentElement)
    {
        if (currentElement is CodeClass { Kind: CodeClassKind.RequestBuilder, Indexer: { } indexer } currentClass
            && indexer.ReturnType.AllTypes.First().TypeDefinition is CodeClass idxReturn
            && idxReturn.UnorderedProperties.Any())
        {
            // Handles possible conflicts when executable URLs like:
            // GET /users/{user-id}/directReports/graph.orgContact
            // GET /users/{user-id}/directReports/{directoryObject-id}/graph.orgContact
            // would resolve to the same command. i.e.:
            // mgc users direct-reports graph-org-contact get*

            // The conflicting nav property will be renamed so that we have 2 commands:
            // mgc users direct-reports graph-org-contact get*
            // mgc users direct-reports graph-org-contact-by-id get*

            // Find matching nav properties between currentClass' nav & indexer return's nav.
            var propsInClass = currentClass.UnorderedProperties
                .Where(static m => m.Kind is CodePropertyKind.RequestBuilder)
                .ToDictionary(static m => m.Name.CleanupSymbolName(), StringComparer.OrdinalIgnoreCase);
            var matchesInIndexer = idxReturn.UnorderedProperties
                .Where(p => p.Kind is CodePropertyKind.RequestBuilder &&
                            propsInClass.ContainsKey(p.Name.CleanupSymbolName()));

            foreach (var matchInIdx in matchesInIndexer)
            {
                if (matchInIdx.Type.AllTypes.First().TypeDefinition is not CodeClass ccIdx
                    || propsInClass[matchInIdx.Name].Type.AllTypes.First().TypeDefinition is not CodeClass ccClass)
                {
                    continue;
                }

                // Check for executable command matches
                // This list is usually small. Up to a max of ~9 for each HTTP method
                // In reality, most instances would have 1 - 3 methods
                var lookup = ccClass.UnorderedMethods
                    .Where(static m => m.Kind is CodeMethodKind.RequestExecutor)
                    .Select(static m => m.Name.CleanupSymbolName())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (ccIdx.UnorderedMethods.Any(m => m.Kind is CodeMethodKind.RequestExecutor
                    && (lookup.Contains(m.Name.CleanupSymbolName()))))
                {
                    matchInIdx.Name = $"{matchInIdx.Name}-ById";
                }
            }
        }

        CrawlTree(currentElement, RenameDuplicateIndexerNavProperties);
    }

    private static void RenameMatchingSubsequentNavCommands(CodeElement currentElement)
    {
        // System.Commandline library doesn't support situations where a
        // command name matches its parent's name. e.g. mgc sites sites *

        // https://github.com/microsoftgraph/msgraph-cli/issues/322
        // https://github.com/microsoftgraph/msgraph-cli/issues/316
        // https://github.com/dotnet/command-line-api/issues/2260

        // To work around that limitation, this command renames any nav
        // property that will result in a command matching its parent's name by
        // adding a 'sub-' prefix.

        // Should handle situations like:
        // GET /sites/{site-id}/sites
        // and generate the command:
        // mgc sites sub-sites get
        // instead of
        // mgc sites sites get
        if (currentElement is CodeProperty
            {
                Kind: CodePropertyKind.RequestBuilder, Type: CodeType
                {
                    TypeDefinition: CodeClass
                    {
                        Kind: CodeClassKind.RequestBuilder, Indexer: { } indexer
                    }
                }
            } currentProp && indexer.ReturnType.AllTypes.First().TypeDefinition is CodeClass idxReturn
                          && idxReturn.UnorderedProperties.Any())
        {
            // Find matching nav properties in indexer.
            var matchesProp = idxReturn.UnorderedProperties
                .Where(p => p.Kind is CodePropertyKind.RequestBuilder &&
                            p.Name.Equals(currentProp.Name, StringComparison.Ordinal));

            foreach (var matchInIdx in matchesProp)
            {
                matchInIdx.Name = $"sub-{matchInIdx.Name}";
            }
        }

        CrawlTree(currentElement, RenameMatchingSubsequentNavCommands);
    }

    private static void CreateCommandBuilders(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.Kind is CodeClassKind.RequestBuilder)
        {
            // Remove request executor
            RemoveUnusedParameters(currentClass);

            // Clone executors & convert to build command
            var requestExecutors = currentClass.UnorderedMethods
                .Where(static e => e.Kind is CodeMethodKind.RequestExecutor);
            CreateCommandBuildersFromRequestExecutors(currentClass, currentClass.Indexer != null, requestExecutors);

            // Replace Nav Properties with BuildXXXCommand methods
            var navProperties = currentClass.UnorderedProperties
                .Where(static e => e.Kind is CodePropertyKind.RequestBuilder);
            CreateCommandBuildersFromNavProps(currentClass, navProperties);

            var requestsWithParams = currentClass.UnorderedMethods
                .Where(static m => m.Kind is CodeMethodKind.RequestBuilderWithParameters);
            CreateCommandBuildersFromRequestBuildersWithParameters(currentClass, requestsWithParams);

            // Add build command for indexers. If an indexer's type has methods with the same name, they will be skipped.
            // Deduplication is managed in method writer.
            if (currentClass.Indexer is { } idx)
            {
                CreateCommandBuildersFromIndexer(currentClass, idx);
            }

            // Build root command
            var clientConstructor = currentClass.UnorderedMethods.FirstOrDefault(static m => m.Kind is CodeMethodKind.ClientConstructor);
            if (clientConstructor != null)
            {
                var rootMethod = new CodeMethod
                {
                    Name = "BuildRootCommand",
                    Documentation = (CodeDocumentation)clientConstructor.Documentation.Clone(),
                    IsAsync = false,
                    Kind = CodeMethodKind.CommandBuilder,
                    ReturnType = new CodeType { Name = "Command", IsExternal = true },
                    OriginalMethod = clientConstructor,
                };
                currentClass.AddMethod(rootMethod);
            }
        }
        CrawlTree(currentElement, CreateCommandBuilders);
    }

    private static void RemoveUnusedParameters(CodeClass currentClass)
    {
        var requestAdapters = currentClass.UnorderedProperties.Where(static p => p.IsOfKind(UnusedPropKinds));
        currentClass.RemoveChildElement(requestAdapters.ToArray());
        var constructorsWithAdapter = currentClass.UnorderedMethods.Where(static m => m.IsOfKind(ConstructorKinds) && m.Parameters.Any(static p => p.IsOfKind(UnusedParamKinds)));
        foreach (var method in constructorsWithAdapter)
        {
            method.RemoveParametersByKind(UnusedParamKinds);
        }
    }

    private static void CreateCommandBuildersFromRequestExecutors(CodeClass currentClass, bool classHasIndexers, IEnumerable<CodeMethod> requestMethods)
    {
        foreach (var requestMethod in requestMethods)
        {
            var clone = (CodeMethod)requestMethod.Clone();
            var cmdName = clone.Name;
            if (clone.HttpMethod is { } m)
            {
                cmdName = GetCommandNameFromHttpMethod(m, classHasIndexers);
            }

            clone.IsAsync = false;
            clone.Name = $"Build{cmdName.CleanupSymbolName().ToFirstCharacterUpperCase()}Command";
            clone.Documentation = (CodeDocumentation)requestMethod.Documentation.Clone();
            clone.ReturnType = CreateCommandType();
            clone.Kind = CodeMethodKind.CommandBuilder;
            clone.OriginalMethod = requestMethod;
            clone.SimpleName = cmdName.CleanupSymbolName();
            clone.ClearParameters();
            currentClass.AddMethod(clone);
            currentClass.RemoveChildElement(requestMethod);
        }
    }

    private static string GetCommandNameFromHttpMethod(HttpMethod httpMethod, bool classHasIndexers)
    {
        return httpMethod switch
        {
            HttpMethod.Get when classHasIndexers => "List",
            HttpMethod.Post when classHasIndexers => "Create",
            _ => httpMethod.ToString(),
        };
    }

    private static void CreateCommandBuildersFromIndexer(CodeClass currentClass, CodeIndexer indexer)
    {
        var collectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        var method = new CodeMethod
        {
            Name = "BuildCommand",
            IsAsync = false,
            Kind = CodeMethodKind.CommandBuilder,
            OriginalIndexer = indexer,
            Documentation = (CodeDocumentation)indexer.Documentation.Clone(),
            // ReturnType setter assigns the parent
            ReturnType = new CodeType
            {
                Name = "Tuple",
                IsExternal = true,
                GenericTypeParameterValues = [
                    CreateCommandType(collectionKind),
                    CreateCommandType(collectionKind),
                ]
            },
            SimpleName = indexer.Name.CleanupSymbolName()
        };

        currentClass.AddMethod(method);
        currentClass.RemoveChildElement(indexer);
    }

    private static void CreateCommandBuildersFromRequestBuildersWithParameters(CodeClass currentClass, IEnumerable<CodeMethod> requestBuildersWithParams)
    {
        foreach (var requestBuilder in requestBuildersWithParams)
        {
            var method = new CodeMethod
            {
                IsAsync = false,
                Name = $"Build{requestBuilder.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}RbCommand",
                Kind = CodeMethodKind.CommandBuilder,
                Documentation = (CodeDocumentation)requestBuilder.Documentation.Clone(),
                ReturnType = CreateCommandType(),
                OriginalMethod = requestBuilder,
                SimpleName = requestBuilder.Name.CleanupSymbolName(),
                Parent = currentClass
            };

            // Ensure constructor parameters are removed
            if (requestBuilder.ReturnType is CodeType ct && ct.TypeDefinition is CodeClass cc)
            {
                var constructors = cc.UnorderedMethods.Where(static m => m.Kind is CodeMethodKind.Constructor);
                foreach (var item in constructors)
                {
                    item.RemoveParametersByKind(CodeParameterKind.Path);
                }
            }
            currentClass.AddMethod(method);
            currentClass.RemoveChildElement(requestBuilder);
        }
    }

    private static void CreateCommandBuildersFromNavProps(CodeClass currentClass, IEnumerable<CodeProperty> navProperties)
    {
        foreach (var navProperty in navProperties)
        {
            var method = new CodeMethod
            {
                IsAsync = false,
                Name = $"Build{navProperty.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}NavCommand",
                Kind = CodeMethodKind.CommandBuilder,
                Documentation = (CodeDocumentation)navProperty.Documentation.Clone(),
                ReturnType = CreateCommandType(),
                AccessedProperty = navProperty,
                SimpleName = navProperty.Name.CleanupSymbolName(),
                Parent = currentClass
            };
            currentClass.AddMethod(method);

            // Remove renamed elements as well
            currentClass.RemoveChildElementByName(navProperty.Name
                .Replace("-ById", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("sub-", string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static CodeType CreateCommandType(CodeTypeBase.CodeTypeCollectionKind collectionKind = CodeTypeBase.CodeTypeCollectionKind.None)
    {
        return new CodeType
        {
            Name = "Command",
            IsExternal = true,
            CollectionKind = collectionKind,
        };
    }

    private static readonly AdditionalUsingEvaluator[] additionalUsingEvaluators =
    {
        new(x => x is CodeClass { Kind: CodeClassKind.RequestBuilder },
            "System.CommandLine", "Command", "RootCommand", "IConsole"),
        new(x => x is CodeClass { Kind: CodeClassKind.RequestBuilder },
            "Microsoft.Kiota.Cli.Commons.IO", "IOutputFormatter", "IOutputFormatterFactory", "FormatterType",
            "PageLinkData", "IPagingService"),
        new(x => x is CodeClass { Kind: CodeClassKind.RequestBuilder },
            "System.Text", "Encoding"),
        new(x => x is CodeMethod { Kind: CodeMethodKind.RequestExecutor or CodeMethodKind.RequestGenerator },
            "Microsoft.Kiota.Cli.Commons.Extensions", "GetRequestAdapter")
    };
}
