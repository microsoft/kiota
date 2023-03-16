﻿using System;
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

    private static int conflictsCount = 0;
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
            ProbeForConflicts(generatedCode);
            if (conflictsCount > 0)
            {
                // Warn on conflicts.
                Console.WriteLine($"Found {conflictsCount} command conflicts.");
            }
        }, cancellationToken);
    }
    private static void ProbeForConflicts(CodeElement currentElement)
    {
        // Warn about potential for duplicates e.g. GET /drives vs GET /drives/{drive-id}/list
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var commandBuilders = currentClass.GetChildElements().OfType<CodeMethod>()
                    .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder));
            Dictionary<string, CodeMethod> classCommands = new(StringComparer.OrdinalIgnoreCase);

            static string GetElementParentClass(in CodeMethod element)
            {
                if (element.Parent is CodeElement e)
                {
                    CodeElement? classParent = e.Parent;
                    LinkedList<string> nameLl = new();
                    nameLl.AddLast(e.Name);
                    while (classParent is not CodeNamespace && classParent is not null)
                    {
                        nameLl.AddFirst(classParent.Name);

                        classParent = classParent?.Parent;
                    }

                    if (!string.IsNullOrEmpty(classParent?.Name))
                    {
                        nameLl.AddFirst(classParent.Name);
                    }
                    return string.Join(".", nameLl);
                }
                else
                {
                    return "N/A";
                }
            }

            static void AddOrWarn(in string name, in CodeMethod c, ref Dictionary<string, CodeMethod> table)
            {
                if (!table.TryAdd(name, c))
                {
                    // Adding the method failed.
                    var existing = table[name];
                    if (c != existing)
                    {
                        // WARN...
                        var warning = $"Warning: Conflicting commands named {name}.";
                        conflictsCount++;
                        Console.WriteLine(warning);
                        Console.WriteLine($"New: {GetElementParentClass(c)}.{c.Name}\nExisting: {GetElementParentClass(existing)}.{existing.Name}");
                    }
                }
            }

            foreach (var cmd in commandBuilders)
            {
                var name = cmd.SimpleName;
                if (cmd.HttpMethod is null)
                {
                    if (cmd.OriginalMethod?.Kind == CodeMethodKind.ClientConstructor)
                    {
                        AddOrWarn("__root__", cmd, ref classCommands);
                    }
                    else if (cmd.OriginalIndexer is not null)
                    {
                        var subCmds = cmd.OriginalIndexer.ReturnType.AllTypes.First()
                                .TypeDefinition?.GetChildElements(true)
                                .OfType<CodeMethod>()
                                .Where(static m => m.IsOfKind(CodeMethodKind.CommandBuilder)) ??
                            Enumerable.Empty<CodeMethod>();
                        foreach (var subCmd in subCmds)
                        {
                            AddOrWarn(subCmd.SimpleName, subCmd, ref classCommands);
                        }
                    }

                }
                else
                {
                    AddOrWarn(name, cmd, ref classCommands);
                }
            }
        }

        CrawlTree(currentElement, ProbeForConflicts);
    }

    private static void CreateCommandBuilders(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Remove request executor
            RemoveUnusedParameters(currentClass);

            var children = currentClass.GetChildElements();
            var indexers = children.OfType<CodeIndexer>();
            var classHasIndexers = indexers.Any();

            // Clone executors & convert to build command
            var requestExecutors = children.OfType<CodeMethod>()
                .Where(e => e.IsOfKind(CodeMethodKind.RequestExecutor));
            CreateCommandBuildersFromRequestExecutors(currentClass, classHasIndexers, requestExecutors);

            // Replace Nav Properties with BuildXXXCommand methods
            var navProperties = children.OfType<CodeProperty>()
                .Where(e => e.IsOfKind(CodePropertyKind.RequestBuilder));
            foreach (var navProp in navProperties)
            {
                var method = CreateBuildCommandMethod(navProp, currentClass);
                currentClass.AddMethod(method);
                currentClass.RemoveChildElement(navProp);
            }

            // Add build command for indexers. If an indexer's type has methods with the same name, they will be skipped.
            // Deduplication is managed in method writer.
            CreateCommandBuildersFromIndexers(currentClass, indexers);

            // Build root command
            var clientConstructor = children.OfType<CodeMethod>().FirstOrDefault(m => m.IsOfKind(CodeMethodKind.ClientConstructor));
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
            var cmdName = clone.Name;
            if (clone.HttpMethod is HttpMethod m)
            {
                cmdName = GetCommandNameFromHttpMethod(m, classHasIndexers);
            }

            clone.IsAsync = false;
            clone.Name = $"Build{cmdName.CleanupSymbolName().ToFirstCharacterUpperCase()}Command";
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

    private static string GetCommandNameFromHttpMethod(HttpMethod httpMethod, bool classHasIndexers)
    {
        return httpMethod switch
        {
            HttpMethod.Get when classHasIndexers => "List",
            HttpMethod.Post when classHasIndexers => "Create",
            _ => httpMethod.ToString(),
        };
    }

    private static void CreateCommandBuildersFromIndexers(CodeClass currentClass, IEnumerable<CodeIndexer> indexers)
    {
        foreach (var indexer in indexers)
        {
            var method = new CodeMethod
            {
                Name = $"BuildCommand",
                IsAsync = false,
                Kind = CodeMethodKind.CommandBuilder,
                OriginalIndexer = indexer,
                Documentation = (CodeDocumentation)indexer.Documentation.Clone(),
                // ReturnType setter assigns the parent
                ReturnType = CreateCommandType(),
                SimpleName = indexer.Name
            };
            method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;

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
            Name = $"Build{navProperty.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}Command",
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
