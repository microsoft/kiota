using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class ShellRefiner : CSharpRefiner, ILanguageRefiner
{
    private static readonly CodePropertyKind[] UnusedPropKinds = new[] { CodePropertyKind.RequestAdapter };
    private static readonly CodeParameterKind[] UnusedParamKinds = new[] { CodeParameterKind.RequestAdapter };
    private static readonly CodeMethodKind[] UnusedMethodKinds = new[] { CodeMethodKind.RequestBuilderWithParameters };
    private static readonly CodeMethodKind[] ConstructorKinds = new[] { CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor };
    public ShellRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = "Microsoft.Kiota.Abstractions",
                        IsExternal = true
                    }
                });
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
                _configuration.UsesBackingStore
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
                new HashSet<Type> { typeof(CodeClass), typeof(ClassDeclaration), typeof(CodeProperty), typeof(CodeUsing), typeof(CodeNamespace), typeof(CodeMethod), typeof(CodeEnum) }
            );
            ReplaceReservedNames(
                generatedCode,
                new CSharpReservedClassNamesProvider(),
                x => $"{x.ToFirstCharacterUpperCase()}Escaped"
            );
            // Replace the reserved types
            ReplaceReservedModelTypes(generatedCode, new CSharpReservedTypesProvider(), x => $"{x}Object");
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNamespaceTypeNames(generatedCode, new CSharpReservedTypesProvider(), static x => $"{x}Namespace");
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
            cancellationToken.ThrowIfCancellationRequested();
            CreateCommandBuilders(generatedCode);
        }, cancellationToken);
    }

    private static void CreateCommandBuilders(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Remove request executor
            RemoveUnusedParameters(currentClass);
            // Replace Nav Properties with BuildXXXCommand methods
            var navProperties = currentClass.GetChildElements().OfType<CodeProperty>().Where(e => e.IsOfKind(CodePropertyKind.RequestBuilder));
            foreach (var navProp in navProperties)
            {
                var method = CreateBuildCommandMethod(navProp, currentClass);
                currentClass.AddMethod(method);
                currentClass.RemoveChildElement(navProp);
            }

            // Build command for indexers
            var indexers = currentClass.GetChildElements().OfType<CodeIndexer>();
            var classHasIndexers = indexers.Any();
            CreateCommandBuildersFromIndexers(currentClass, indexers);

            // Clone executors & convert to build command
            var requestExecutors = currentClass.GetChildElements().OfType<CodeMethod>().Where(e => e.IsOfKind(CodeMethodKind.RequestExecutor));
            CreateCommandBuildersFromRequestExecutors(currentClass, classHasIndexers, requestExecutors);

            // Build root command
            var clientConstructor = currentClass.GetChildElements().OfType<CodeMethod>().FirstOrDefault(m => m.IsOfKind(CodeMethodKind.ClientConstructor));
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
        var requestAdapters = currentClass.Properties.Where(static p => p.IsOfKind(UnusedPropKinds));
        currentClass.RemoveChildElement(requestAdapters.ToArray());
        var constructorsWithAdapter = currentClass.Methods.Where(static m => m.IsOfKind(ConstructorKinds) && m.Parameters.Any(static p => p.IsOfKind(UnusedParamKinds)));
        foreach (var method in constructorsWithAdapter)
        {
            method.RemoveParametersByKind(UnusedParamKinds);
        }

        var unwantedMethods = currentClass.Methods.Where(static m => m.IsOfKind(UnusedMethodKinds));
        currentClass.RemoveChildElement(unwantedMethods.ToArray());
    }

    private static void CreateCommandBuildersFromRequestExecutors(CodeClass currentClass, bool classHasIndexers, IEnumerable<CodeMethod> requestMethods)
    {
        foreach (var requestMethod in requestMethods)
        {
            var clone = (CodeMethod)requestMethod.Clone();
            var cmdName = clone.HttpMethod switch
            {
                HttpMethod.Get when classHasIndexers => "List",
                HttpMethod.Post when classHasIndexers => "Create",
                _ => clone.Name,
            };

            clone.IsAsync = false;
            clone.Name = $"Build{cmdName}Command";
            clone.Documentation = (CodeDocumentation)requestMethod.Documentation.Clone();
            clone.ReturnType = CreateCommandType();
            clone.Kind = CodeMethodKind.CommandBuilder;
            clone.OriginalMethod = requestMethod;
            clone.SimpleName = cmdName;
            clone.ClearParameters();
            currentClass.AddMethod(clone);
            currentClass.RemoveChildElement(requestMethod);
        }
    }

    private static void CreateCommandBuildersFromIndexers(CodeClass currentClass, IEnumerable<CodeIndexer> indexers)
    {
        foreach (var indexer in indexers)
        {
            var method = new CodeMethod
            {
                Name = "BuildCommand",
                IsAsync = false,
                Kind = CodeMethodKind.CommandBuilder,
                OriginalIndexer = indexer,
                Documentation = (CodeDocumentation)indexer.Documentation.Clone(),
                // ReturnType setter assigns the parent
                ReturnType = CreateCommandType()
            };
            currentClass.AddMethod(method);
            currentClass.RemoveChildElement(indexer);
        }
    }

    private static CodeType CreateCommandType()
    {
        return new CodeType
        {
            Name = "Command",
            IsExternal = true,
        };
    }

    private static CodeMethod CreateBuildCommandMethod(CodeProperty navProperty, CodeClass parent)
    {
        var codeMethod = new CodeMethod
        {
            IsAsync = false,
            Name = $"Build{navProperty.Name.ToFirstCharacterUpperCase()}Command",
            Kind = CodeMethodKind.CommandBuilder,
            Documentation = (CodeDocumentation)navProperty.Documentation.Clone(),
            ReturnType = CreateCommandType(),
            AccessedProperty = navProperty,
            SimpleName = navProperty.Name,
            Parent = parent
        };
        return codeMethod;
    }

    private static readonly AdditionalUsingEvaluator[] additionalUsingEvaluators = {
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "System.CommandLine",  "Command", "RootCommand", "IConsole"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "Microsoft.Kiota.Cli.Commons.IO", "IOutputFormatter", "IOutputFormatterFactory", "FormatterType", "PageLinkData", "IPagingService"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "Microsoft.Extensions.Hosting", "IHost"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "Microsoft.Extensions.DependencyInjection", "IHost"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
            "System.Text",  "Encoding"),
        new (x => {
            return x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator);
        } , "Microsoft.Kiota.Cli.Commons.Extensions", "GetRequestAdapter")
    };
}
