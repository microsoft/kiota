using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Refiners;

public class GoRefiner : CommonLanguageRefiner
{
    public GoRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        _configuration.NamespaceNameSeparator = "/";
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeduplicateErrorMappings(generatedCode);
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = "github.com/microsoft/kiota-abstractions-go",
                        IsExternal = true
                    }
                },
                accessModifier: AccessModifier.Public);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                false,
                static x => $"By{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterLowerCase(),
                GenerationLanguage.Go);
            FlattenNestedHierarchy(generatedCode);
            FlattenParamsFileNames(generatedCode);
            FlattenFileNames(generatedCode);
            NormalizeNamespaceNames(generatedCode);
            AddInnerClasses(
                generatedCode,
                true,
                string.Empty,
                false,
                MergeOverLappedStrings);
            if (_configuration.ExcludeBackwardCompatible) //TODO remove condition for v2
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
                    usingForDefaultGenericParameter: new CodeUsing
                    {
                        Name = "DefaultQueryParameters",
                        Declaration = new CodeType
                        {
                            Name = AbstractionsNamespaceName,
                            IsExternal = true
                        }
                    });
            RenameCancellationParameter(generatedCode);
            RemoveDiscriminatorMappingsTargetingSubNamespaces(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceRequestBuilderPropertiesByMethods(
                generatedCode
            );
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                static s => s,
                true,
                string.Empty,
                string.Empty,
                "GetIsComposedType"
            );
            cancellationToken.ThrowIfCancellationRequested();
            RemoveModelPropertiesThatDependOnSubNamespaces(
                generatedCode
            );
            FixConstructorClashes(generatedCode, x => $"{x}Escaped");
            ReplaceReservedNames(
                generatedCode,
                new GoNamespaceReservedNamesProvider(),
                x => $"{x}Escaped",
                shouldReplaceCallback: x => x is CodeNamespace
            );
            ReplaceReservedNames(
                generatedCode,
                new GoReservedNamesProvider(),
                x => $"{x}Escaped",
                shouldReplaceCallback: x => (x is not CodeNamespace) &&
                                            (x is not CodeEnumOption && x is not CodeEnum) && // enums and enum options start with uppercase
                                            (x is not CodeProperty currentProp ||
                                            !(currentProp.Parent is CodeClass parentClass &&
                                            parentClass.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.ParameterSet) &&
                                            currentProp.Access == AccessModifier.Public))); // Go reserved keywords are all lowercase and public properties are uppercased when we don't provide accessors (models)
            // Replace reserved names in method parameters
            ReplaceReservedParameterNames(generatedCode, new GoReservedNamesProvider(), x => $"{x}Escaped");
            ReplaceReservedExceptionPropertyNames(generatedCode, new GoExceptionsReservedNamesProvider(), x => $"{x}Escaped");
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(
                generatedCode,
                true,
                false,
                true);
            AddDefaultImports(
                generatedCode,
                defaultUsingEvaluators);
            CorrectCoreType(
                generatedCode,
                CorrectMethodType,
                CorrectPropertyType,
                CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            DisableActionOf(generatedCode,
                CodeParameterKind.RequestConfiguration);
            AddGetterAndSetterMethods(
                generatedCode,
                new() {
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.Custom,
                    CodePropertyKind.BackingStore },
                static (element, s) =>
                {
                    var refinedName = s.ToPascalCase(UnderscoreArray);
                    if (element.Parent is CodeClass parentClass &&
                        parentClass.FindChildByName<CodeProperty>(refinedName) is not null)
                    {
                        return s;
                    }
                    return refinedName;
                },
                _configuration.UsesBackingStore,
                false,
                "Get",
                "Set");
            AddConstructorsForDefaultValues(
                generatedCode,
                true,
                true,  //forcing add as constructors are required for by factories
                new[] { CodeClassKind.RequestConfiguration });
            cancellationToken.ThrowIfCancellationRequested();
            MakeModelPropertiesNullable(
                generatedCode);
            AddErrorAndStringsImportForEnums(
                generatedCode);
            var defaultConfiguration = new GenerationConfiguration();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "github.com/microsoft/kiota-serialization-json-go.JsonSerializationWriterFactory",
                    "github.com/microsoft/kiota-serialization-text-go.TextSerializationWriterFactory",
                    "github.com/microsoft/kiota-serialization-form-go.FormSerializationWriterFactory",
                    "github.com/microsoft/kiota-serialization-multipart-go.MultipartSerializationWriterFactory",
                });
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "github.com/microsoft/kiota-serialization-json-go.JsonParseNodeFactory",
                    "github.com/microsoft/kiota-serialization-text-go.TextParseNodeFactory",
                    "github.com/microsoft/kiota-serialization-form-go.FormParseNodeFactory",
                });
            AddSerializationModulesImport(
                generatedCode,
                ["github.com/microsoft/kiota-abstractions-go/serialization.SerializationWriterFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultSerializer"],
                ["github.com/microsoft/kiota-abstractions-go/serialization.ParseNodeFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultDeserializer"]);
            cancellationToken.ThrowIfCancellationRequested();
            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiError",
                    "github.com/microsoft/kiota-abstractions-go"
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                true
            );
            AddParsableImplementsForModelClasses(
                generatedCode,
                "Parsable"
            );
            RenameInnerModelsToAppended(
                generatedCode
            );
            cancellationToken.ThrowIfCancellationRequested();
            CorrectCyclicReference(generatedCode);
            CopyModelClassesAsInterfaces(
                generatedCode,
                x => $"{x.Name}able"
            );
            AddContextParameterToGeneratorMethods(generatedCode);
            CorrectTypes(generatedCode);
            CorrectCoreTypesForBackingStore(generatedCode, $"{conventions.StoreHash}.BackingStoreFactoryInstance()", false);
            CorrectBackingStoreTypes(generatedCode);
            ReplacePropertyNames(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                },
                static s => s.ToFirstCharacterUpperCase());
            AddPrimaryErrorMessage(generatedCode,
                "Error",
                () => new CodeType { Name = "string", IsNullable = false, IsExternal = true }
            );
            GenerateCodeFiles(generatedCode);
        }, cancellationToken);
    }

    /// <summary>
    /// Replaces reserved names in method parameters specifically, since they are not part of the regular tree traversal
    /// </summary>
    private static void ReplaceReservedParameterNames(CodeElement currentElement, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        if (currentElement is CodeMethod method)
        {
            foreach (var parameter in method.Parameters)
            {
                if (provider.ReservedNames.Contains(parameter.Name))
                {
                    parameter.Name = replacement(parameter.Name);
                }
            }
        }
        CrawlTree(currentElement, element => ReplaceReservedParameterNames(element, provider, replacement));
    }

    private void CorrectCyclicReference(CodeElement currentElement)
    {
        var currentNameSpace = currentElement.GetImmediateParentOfType<CodeNamespace>();
        var modelsNameSpace = findClientNameSpace(currentNameSpace)
            ?.FindNamespaceByName(
                $"{_configuration.ClientNamespaceName}.{GenerationConfiguration.ModelsNamespaceSegmentName}");

        if (modelsNameSpace == null)
            return;

        var dependencies = new Dictionary<string, HashSet<string>>();
        GetUsingsInModelsNameSpace(modelsNameSpace, modelsNameSpace, dependencies);

        var migratedNamespaces = new Dictionary<string, string>();
        var cycles = FindCycles(dependencies);
        foreach (var cycle in cycles)
        {
            foreach (var cycleReference in cycle.Value)
            {
                var dupNs = cycleReference[^2]; // 2nd last element is target base namespace
                var nameSpace = modelsNameSpace.FindNamespaceByName(dupNs, true);

                migratedNamespaces[dupNs] = modelsNameSpace.Name;
                MigrateNameSpace(nameSpace!, modelsNameSpace);

                if (!cycle.Key.Equals(modelsNameSpace.Name, StringComparison.OrdinalIgnoreCase)
                    && !migratedNamespaces.ContainsKey(cycle.Key)
                    && modelsNameSpace.FindNamespaceByName(cycle.Key, true) is { } currentNs)
                {
                    migratedNamespaces[cycle.Key] = modelsNameSpace.Name;
                    MigrateNameSpace(currentNs, modelsNameSpace);
                }
            }
        }

        CorrectReferencesToMigratedModels(currentElement, migratedNamespaces);
    }

    private string GetComposedName(CodeElement codeClass)
    {
        var classNameList = getPathsName(codeClass, codeClass.Name);
        return string.Join(string.Empty, classNameList.Count > 1 ? classNameList.Skip(1) : classNameList);
    }

    private static void GetUsingsInModelsNameSpace(CodeNamespace modelsNameSpace, CodeNamespace currentNameSpace, Dictionary<string, HashSet<string>> dependencies)
    {
        if (!modelsNameSpace.Name.Equals(currentNameSpace.Name, StringComparison.OrdinalIgnoreCase) && !currentNameSpace.IsChildOf(modelsNameSpace))
            return;

        dependencies[currentNameSpace.Name] = currentNameSpace.Classes
            .SelectMany(static codeClass => codeClass.Usings)
            .Where(static x => x.Declaration != null && !x.Declaration.IsExternal)
            .Select(static x => x.Declaration?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())
            .OfType<CodeNamespace>()
            .Where(ns => !ns.Name.Equals(currentNameSpace.Name, StringComparison.OrdinalIgnoreCase))
            .Select(static ns => ns.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var codeNameSpace in currentNameSpace.Namespaces.Where(codeNameSpace => !dependencies.ContainsKey(codeNameSpace.Name)))
        {
            GetUsingsInModelsNameSpace(modelsNameSpace, codeNameSpace, dependencies);
        }
    }

    /// <summary>
    /// Returns a dictionary of cycles in the graph with the key being the base namespace and the values being the path to the cycle
    /// In GoLang, any self referencing namespace in a tree is a cycle, therefore the whole namespace is moved to the root
    /// </summary>
    /// <param name="dependencies"></param>
    /// <returns></returns>
    private static Dictionary<string, List<List<string>>> FindCycles(Dictionary<string, HashSet<string>> dependencies)
    {
        var cycles = new Dictionary<string, List<List<string>>>();
        var visited = new HashSet<string>();
        var stack = new Stack<string>();

        foreach (var node in dependencies.Keys)
        {
            if (!visited.Contains(node))
            {
                SearchCycles(node, node, dependencies, visited, stack, cycles);
            }
        }

        return cycles;
    }


    /// <summary>
    /// Performs a DFS search to find cycles in the graph. Method will stop at the first cycle found in each node
    /// </summary>
    private static void SearchCycles(string parentNode, string node, Dictionary<string, HashSet<string>> dependencies, HashSet<string> visited, Stack<string> stack, Dictionary<string, List<List<string>>> cycles)
    {
        visited.Add(node);
        stack.Push(node);

        if (dependencies.TryGetValue(node, out var value))
        {
            var stackSet = new HashSet<string>(stack);
            foreach (var neighbor in value)
            {
                if (stackSet.Contains(neighbor))
                {
                    var cycle = stack.Reverse().Concat([neighbor]).ToList();
                    if (!cycles.ContainsKey(parentNode))
                        cycles[parentNode] = new List<List<string>>();

                    cycles[parentNode].Add(cycle);
                }
                else if (!visited.Contains(neighbor))
                {
                    SearchCycles(parentNode, neighbor, dependencies, visited, stack, cycles);
                }
            }
        }

        stack.Pop();
    }

    private void MigrateNameSpace(CodeNamespace currentNameSpace, CodeNamespace targetNameSpace)
    {
        foreach (var codeClass in currentNameSpace.Classes)
        {
            currentNameSpace.RemoveChildElement(codeClass);
            codeClass.Name = GetComposedName(codeClass);
            codeClass.Parent = targetNameSpace;
            targetNameSpace.AddClass(codeClass);
        }

        foreach (var x in currentNameSpace.Enums)
        {
            currentNameSpace.RemoveChildElement(x);
            x.Name = GetComposedName(x);
            x.Parent = targetNameSpace;
            targetNameSpace.AddEnum(x);
        }

        foreach (var x in currentNameSpace.Interfaces)
        {
            currentNameSpace.RemoveChildElement(x);
            x.Name = GetComposedName(x);
            x.Parent = targetNameSpace;
            targetNameSpace.AddInterface(x);
        }

        foreach (var x in currentNameSpace.Functions)
        {
            currentNameSpace.RemoveChildElement(x);
            x.Name = GetComposedName(x);
            x.Parent = targetNameSpace;
            targetNameSpace.AddFunction(x);
        }

        foreach (var x in currentNameSpace.Constants)
        {
            currentNameSpace.RemoveChildElement(x);
            x.Name = GetComposedName(x);
            x.Parent = targetNameSpace;
            targetNameSpace.AddConstant(x);
        }

        foreach (var ns in currentNameSpace.Namespaces)
        {
            MigrateNameSpace(ns, targetNameSpace);
        }
    }
    private static void CorrectReferencesToMigratedModels(CodeElement currentElement, Dictionary<string, string> migratedNamespaces)
    {
        if (currentElement is CodeNamespace cn)
        {
            var usings = cn.GetChildElements()
                .SelectMany(static x => x.GetChildElements())
                .OfType<ProprietableBlockDeclaration>()
                .SelectMany(static x => x.Usings)
                .Where(x => migratedNamespaces.ContainsKey(x.Name))
                .ToArray();

            foreach (var codeUsing in usings)
            {
                if (codeUsing.Parent is not ProprietableBlockDeclaration blockDeclaration ||
                    codeUsing.Declaration?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>().Name.Equals(migratedNamespaces[codeUsing.Name], StringComparison.OrdinalIgnoreCase) == false
                    )
                {
                    continue;
                }

                blockDeclaration.RemoveUsings(codeUsing);
                blockDeclaration.AddUsings(new CodeUsing
                {
                    Name = migratedNamespaces[codeUsing.Name],
                    Declaration = codeUsing.Declaration
                });
            }
        }

        CrawlTree(currentElement, x => CorrectReferencesToMigratedModels(x, migratedNamespaces));
    }
    private void GenerateCodeFiles(CodeElement currentElement)
    {
        if (currentElement is CodeInterface codeInterface && currentElement.Parent is CodeNamespace codeNamespace)
        {
            var modelName = codeInterface.Name.TrimSuffix("able");
            var modelClass = codeNamespace.FindChildByName<CodeClass>(modelName, false) ??
                             codeNamespace.FindChildByName<CodeClass>(modelName.ToFirstCharacterUpperCase(), false);
            if (modelClass != null)
            {
                codeNamespace.TryAddCodeFile(modelName, modelClass, codeInterface);
            }

        }
        CrawlTree(currentElement, GenerateCodeFiles);
    }

    private string MergeOverLappedStrings(string start, string end)
    {
        var search = "RequestBuilder";
        start = start.ToFirstCharacterUpperCase();
        end = end.ToFirstCharacterUpperCase();
        var endPattern = end.Contains(search, StringComparison.OrdinalIgnoreCase) ? end[..(end.IndexOf(search, StringComparison.OrdinalIgnoreCase) + search.Length)] : end;

        if (start.EndsWith(endPattern, StringComparison.OrdinalIgnoreCase))
            return $"{start[..start.IndexOf(endPattern, StringComparison.OrdinalIgnoreCase)]}{end}";

        return $"{start}{end}";
    }

    private void CorrectBackingStoreTypes(CodeElement currentElement)
    {
        if (!_configuration.UsesBackingStore)
            return;

        if (currentElement is CodeMethod currentMethod)
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.BackingStore)
                                    && currentMethod.IsOfKind(CodeMethodKind.ClientConstructor)
                                    && x.Type is CodeType).ToList().ForEach(x =>
            {
                var type = (CodeType)x.Type;
                type.Name = "BackingStoreFactory";
                type.IsNullable = false;
                type.IsExternal = true;
            });
        }
        else if (currentElement is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model))
        {
            var propertiesToCorrect = codeClass.Properties
                .Where(static x => x.IsOfKind(CodePropertyKind.Custom))
                .Union(codeClass.Methods
                    .Where(x => x.IsAccessor && (x.AccessedProperty?.IsOfKind(CodePropertyKind.Custom) ?? false))
                    .Select(static x => x.AccessedProperty!))
                .Distinct()
                .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase);

            var currentNameSpace = codeClass.GetImmediateParentOfType<CodeNamespace>();
            var modelsNameSpace = findClientNameSpace(currentNameSpace)
                ?.FindNamespaceByName($"{_configuration.ClientNamespaceName}.{GenerationConfiguration.ModelsNamespaceSegmentName}");

            foreach (var property in propertiesToCorrect)
            {
                if (property.Type is CodeType codeType && codeType.TypeDefinition is CodeClass typeClass)
                {
                    var targetNameSpace = typeClass.GetImmediateParentOfType<CodeNamespace>();
                    var interfaceName = $"{codeType.Name}able";
                    var existing = targetNameSpace.FindChildByName<CodeInterface>(interfaceName, false) ??
                                   targetNameSpace.FindChildByName<CodeInterface>(interfaceName.ToFirstCharacterUpperCase(), false) ??
                                   modelsNameSpace?.FindChildByName<CodeInterface>(interfaceName, false) ??
                                   modelsNameSpace?.FindChildByName<CodeInterface>(interfaceName.ToFirstCharacterUpperCase(), false);

                    if (existing == null)
                        continue;

                    CodeType type = (codeType.Clone() as CodeType)!;
                    type.TypeDefinition = existing;
                    property.Type = type;
                }
            }
        }
        CrawlTree(currentElement, CorrectBackingStoreTypes);
    }

    private static void CorrectTypes(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters) &&
            currentElement.Parent is CodeClass _ &&
            currentMethod.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentNamespace &&
            currentNamespace.Depth > 0 && currentMethod.ReturnType is CodeType ct &&
            ct.TypeDefinition is not null &&
            !ct.Name.Equals(ct.TypeDefinition.Name, StringComparison.Ordinal))
        {
            ct.Name = ct.TypeDefinition.Name;
        }
        CrawlTree(currentElement, CorrectTypes);
    }

    private CodeNamespace? _clientNameSpace;
    private CodeNamespace? findClientNameSpace(CodeElement? currentElement)
    {
        if (_clientNameSpace != null) return _clientNameSpace;
        if (currentElement == null) return null;

        var currentNamespace = currentElement.GetImmediateParentOfType<CodeNamespace>();
        if (currentNamespace != null && (_configuration.ClientNamespaceName.Equals(currentNamespace.Name, StringComparison.OrdinalIgnoreCase) || currentElement.Parent == null))
        {
            _clientNameSpace = currentNamespace;
        }

        return findClientNameSpace(currentElement.Parent);
    }

    private void FlattenNestedHierarchy(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass &&
            codeClass.IsOfKind(CodeClassKind.Model) &&
            codeClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentNamespace &&
            findClientNameSpace(currentNamespace) is CodeNamespace parentNameSpace)
        {
            // if the parent is not the models namespace rename and move it to package root
            var modelNameSpace = parentNameSpace.FindNamespaceByName($"{_configuration.ClientNamespaceName}.{GenerationConfiguration.ModelsNamespaceSegmentName}");
            var packageRootNameSpace = findNameSpaceAtLevel(parentNameSpace, currentNamespace, 1);
            if (!packageRootNameSpace.Name.Equals(currentNamespace.Name, StringComparison.Ordinal) && modelNameSpace != null && !currentNamespace.IsChildOf(modelNameSpace))
            {

                currentNamespace.RemoveChildElement(codeClass);
                codeClass.Name = GetComposedName(codeClass);
                codeClass.Parent = packageRootNameSpace;
                packageRootNameSpace.AddClass(codeClass);
            }
        }

        CrawlTree(currentElement, FlattenNestedHierarchy);
    }

    private void FlattenParamsFileNames(CodeElement currentElement)
    {
        if (currentElement is CodeMethod codeMethod
            && codeMethod.IsOfKind(CodeMethodKind.RequestGenerator, CodeMethodKind.RequestExecutor))
        {
            foreach (var param in codeMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.RequestConfiguration)))
            {
                var nameList = getPathsName(param, param.Type.Name.ToFirstCharacterUpperCase());
                var newTypeName = string.Join(string.Empty, nameList.Count > 1 ? nameList.Skip(1) : nameList);
                foreach (var typeDef in param.Type.AllTypes.Select(static x => x.TypeDefinition).Where(x => !string.IsNullOrEmpty(x?.Name) && !newTypeName.EndsWith(x.Name, StringComparison.OrdinalIgnoreCase)))
                    typeDef!.Name = newTypeName;
            }

        }

        CrawlTree(currentElement, FlattenParamsFileNames);
    }

    private List<string> getPathsName(CodeElement codeClass, string fileName, bool removeDuplicate = true)
    {
        // update the code class name to include the entire path
        var currentNamespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
        var namespacePathSegments = new List<string>(currentNamespace.Name
            .Replace(_configuration.ClientNamespaceName, string.Empty, StringComparison.Ordinal)
            .TrimStart('.')
            .Split('.'));
        // add the namespace to the code class Name
        namespacePathSegments = namespacePathSegments.Select(static x => x.ToFirstCharacterUpperCase().Trim())
            .Where(static x => !string.IsNullOrEmpty(x))
            .ToList();

        // check if the last element contains current name and remove it
        if (namespacePathSegments.Count > 0 && removeDuplicate && fileName.ToFirstCharacterUpperCase().Contains(namespacePathSegments.Last(), StringComparison.Ordinal))
            namespacePathSegments.RemoveAt(namespacePathSegments.Count - 1);

        namespacePathSegments.Add(fileName.ToFirstCharacterUpperCase());
        return namespacePathSegments;
    }

    private static CodeNamespace findNameSpaceAtLevel(CodeNamespace rootNameSpace, CodeNamespace currentNameSpace, int level)
    {
        var namespaceList = new List<CodeNamespace> { currentNameSpace };

        var mySpace = currentNameSpace;
        while (mySpace.Parent is CodeNamespace parentNameSpace && !parentNameSpace.Name.Equals(rootNameSpace.Name, StringComparison.OrdinalIgnoreCase))
        {
            namespaceList.Add(parentNameSpace);
            mySpace = parentNameSpace;
        }

        return namespaceList[^level];
    }

    private void FlattenFileNames(CodeElement currentElement)
    {
        // add the namespace to the name of the code element and the file name
        if (currentElement is CodeClass codeClass
            && codeClass.Parent is CodeNamespace currentNamespace
            && !codeClass.IsOfKind(CodeClassKind.Model)
            && findClientNameSpace(codeClass.Parent) is CodeNamespace rootNameSpace
            && !rootNameSpace.Name.Equals(currentNamespace.Name, StringComparison.Ordinal)
            && !currentNamespace.IsChildOf(rootNameSpace, true))
        {
            var classNameList = getPathsName(codeClass, codeClass.Name.ToFirstCharacterUpperCase());
            var newClassName = string.Join(string.Empty, classNameList.Count > 1 ? classNameList.Skip(1) : classNameList);

            var nextNameSpace = findNameSpaceAtLevel(rootNameSpace, currentNamespace, 1);
            currentNamespace.RemoveChildElement(codeClass);
            codeClass.Name = newClassName;
            codeClass.Parent = nextNameSpace;
            nextNameSpace.AddClass(codeClass);
        }

        CrawlTree(currentElement, FlattenFileNames);
    }

    private static void FixConstructorClashes(CodeElement currentElement, Func<string, string> nameCorrection)
    {
        if (currentElement is CodeNamespace currentNamespace)
            foreach (var codeClassName in currentNamespace
                                        .Classes
                                        .Where(static x => x.Name.StartsWith("New", StringComparison.OrdinalIgnoreCase))
                                        .Select(static x => x.Name)
                                        .ToArray())
            {
                var targetName = codeClassName[3..];
                if (currentNamespace.FindChildByName<CodeClass>(targetName, false) is not null)
                    currentNamespace.RenameChildElement(codeClassName, nameCorrection(codeClassName));
            }
        CrawlTree(currentElement, x => FixConstructorClashes(x, nameCorrection));
    }

    protected static void RenameCancellationParameter(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) && currentMethod.Parameters.OfKind(CodeParameterKind.Cancellation) is CodeParameter parameter)
        {
            parameter.Name = ContextParameterName;
            parameter.Documentation.DescriptionTemplate = ContextVarDescription;
            parameter.Kind = CodeParameterKind.Cancellation;
            parameter.Optional = false;
            parameter.Type.Name = conventions.ContextVarTypeName;
            parameter.Type.IsNullable = false;
        }
        CrawlTree(currentElement, RenameCancellationParameter);
    }
    private const string ContextParameterName = "ctx";
    private const string ContextVarDescription = "Pass a context parameter to the request";
    private static void AddContextParameterToGeneratorMethods(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestGenerator) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.Cancellation) is null)
            currentMethod.AddParameter(new CodeParameter
            {
                Name = ContextParameterName,
                Type = new CodeType
                {
                    Name = conventions.ContextVarTypeName,
                    IsNullable = false,
                },
                Kind = CodeParameterKind.Cancellation,
                Optional = false,
                Documentation = {
                    DescriptionTemplate = ContextVarDescription,
                },
            });
        CrawlTree(currentElement, AddContextParameterToGeneratorMethods);
    }

    private static void RemoveModelPropertiesThatDependOnSubNamespaces(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Parent is CodeNamespace currentNamespace)
        {
            var propertiesToRemove = currentClass.Properties
                                                    .Where(x => x.IsOfKind(CodePropertyKind.Custom) &&
                                                                x.Type is CodeType pType &&
                                                                !pType.IsExternal &&
                                                                pType.TypeDefinition != null &&
                                                                currentNamespace.IsParentOf(pType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>()))
                                                    .ToArray();
            if (propertiesToRemove.Length != 0)
            {
                currentClass.RemoveChildElement(propertiesToRemove);
                var propertiesToRemoveHashSet = propertiesToRemove.ToHashSet();
                var methodsToRemove = currentClass.Methods
                                                    .Where(x => x.IsAccessor &&
                                                            x.AccessedProperty != null &&
                                                            propertiesToRemoveHashSet.Contains(x.AccessedProperty))
                                                    .ToArray();
                currentClass.RemoveChildElement(methodsToRemove);
            }
        }
        CrawlTree(currentElement, RemoveModelPropertiesThatDependOnSubNamespaces);
    }
    private static void ReplaceRequestBuilderPropertiesByMethods(CodeElement currentElement)
    {
        if (currentElement is CodeProperty currentProperty &&
            currentProperty.IsOfKind(CodePropertyKind.RequestBuilder) &&
            currentElement.Parent is CodeClass parentClass)
        {
            parentClass.RemoveChildElement(currentProperty);
            currentProperty.Type.IsNullable = false;
            parentClass.AddMethod(new CodeMethod
            {
                Name = currentProperty.Name,
                ReturnType = currentProperty.Type,
                Access = AccessModifier.Public,
                Documentation = (CodeDocumentation)currentProperty.Documentation.Clone(),
                IsAsync = false,
                Kind = CodeMethodKind.RequestBuilderBackwardCompatibility,
            });
        }
        CrawlTree(currentElement, ReplaceRequestBuilderPropertiesByMethods);
    }
    private static void AddErrorAndStringsImportForEnums(CodeElement currentElement)
    {
        if (currentElement is CodeEnum { Flags: true } currentEnum)
        {
            currentEnum.AddUsing(new CodeUsing
            {
                Name = "strings",
            });
        }
        CrawlTree(currentElement, AddErrorAndStringsImportForEnums);
    }
    private static readonly GoConventionService conventions = new();
    private static readonly HashSet<string> typeToSkipStrConv = new(StringComparer.OrdinalIgnoreCase) {
        "DateTimeOffset",
        "Duration",
        "TimeOnly",
        "DateOnly",
        "TimeSpan",
        "Time",
        "ISODuration",
        "string",
        "UUID",
        "Guid"
    };
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "RequestInformation", "HttpMethod", "RequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor) &&
                    method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path) &&
                                            !typeToSkipStrConv.Contains(x.Type.Name)),
            "strconv", "FormatBool"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) &&
                    method.OriginalIndexer is CodeIndexer indexer && !indexer.IndexParameter.Type.Name.Equals("string", StringComparison.OrdinalIgnoreCase)
                    && !typeToSkipStrConv.Contains(indexer.IndexParameter.Type.Name),
            "strconv", "FormatInt"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "github.com/microsoft/kiota-abstractions-go/serialization", "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            "github.com/microsoft/kiota-abstractions-go/serialization", "ParseNode", "Parsable"),
        new (static x => x is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model),
            SerializationNamespaceName, "Parsable"),
        new (static x => x is CodeMethod method &&
                         method.IsOfKind(CodeMethodKind.RequestGenerator) &&
                         method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.RequestBody) &&
                                                    x.Type.IsCollection &&
                                                    x.Type is CodeType pType &&
                                                    (pType.TypeDefinition is CodeClass ||
                                                     pType.TypeDefinition is CodeInterface)),
            SerializationNamespaceName, "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) &&
                                            (@class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)) ||
                                            @class.StartBlock.Implements.Any(x => KiotaBuilder.AdditionalHolderInterface.Equals(x.Name, StringComparison.OrdinalIgnoreCase))),
            SerializationNamespaceName, "AdditionalDataHolder"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeUnionType unionType && unionType.Types.Any(static y => !y.IsExternal) && unionType.DiscriminatorInformation.HasBasicDiscriminatorInformation,
            "strings", "EqualFold"),
        new (static x => x is CodeMethod method && (method.IsOfKind(CodeMethodKind.RequestExecutor) || method.IsOfKind(CodeMethodKind.RequestGenerator)), "context","*context"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal) && intersectionType.DiscriminatorInformation.HasBasicDiscriminatorInformation,
            SerializationNamespaceName, "MergeDeserializersForIntersectionWrapper"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore), "github.com/microsoft/kiota-abstractions-go/store","BackingStore"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                         method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "github.com/microsoft/kiota-abstractions-go/store", "BackingStoreFactory"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase),
            SerializationNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeMethod @method && @method.IsOfKind(CodeMethodKind.RequestExecutor) && (method.ReturnType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase) ||
                                                                                                        method.Parameters.Any(x => x.Kind is CodeParameterKind.RequestBody && x.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))),
            SerializationNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeEnum @enum && @enum.Flags,"", "math"),
    };
    private const string MultipartBodyClassName = "MultipartBody";
    private const string AbstractionsNamespaceName = "github.com/microsoft/kiota-abstractions-go";
    private const string SerializationNamespaceName = "github.com/microsoft/kiota-abstractions-go/serialization";
    internal const string UntypedNodeName = "UntypedNodeable";

    private void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
        block.ReplaceImplementByName(KiotaBuilder.BackedModelInterface, "BackedModel");
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        var parentClass = currentMethod.Parent as CodeClass;
        if (currentMethod.IsOfKind(CodeMethodKind.RequestGenerator, CodeMethodKind.RequestExecutor))
        {
            if (currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                currentMethod.ReturnType.IsNullable = true;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.RequestBody) is CodeParameter bodyParam && bodyParam.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase))
                bodyParam.Type.IsNullable = false;
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(static x => x.Type.Name.Equals("ISerializationWriter", StringComparison.Ordinal)).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = $"map[string]func({conventions.SerializationHash}.ParseNode)(error)";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor))
        {
            if (currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl) is CodeParameter rawUrlParam)
                rawUrlParam.Type.IsNullable = false;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathsParam)
            {
                pathsParam.Type.Name = "map[string]string";
                pathsParam.Type.IsNullable = true;
            }

            currentMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                .Where(static x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.Factory))
        {
            currentMethod.ReturnType.IsNullable = true;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            {
                parseNodeParam.Type.IsNullable = false;
                parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
            }

            if (currentMethod.IsOfKind(CodeMethodKind.Factory))
                currentMethod.ReturnType = new CodeType { Name = "Parsable", IsNullable = false, IsExternal = true };
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlBuilder))
        {
            currentMethod.ReturnType.IsNullable = true;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl) is CodeParameter codeParameter)
                codeParameter.Type.IsNullable = false;
        }
        CorrectCoreTypes(parentClass, DateTypesReplacements, types: currentMethod.Parameters
                                                .Select(static x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
        {"DateTimeOffset", ("Time", new CodeUsing {
                                        Name = "Time",
                                        Declaration = new CodeType {
                                            Name = "time",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("ISODuration", new CodeUsing {
                                        Name = "ISODuration",
                                        Declaration = new CodeType {
                                            Name = SerializationNamespaceName,
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", (string.Empty, new CodeUsing {
                                Name = "DateOnly",
                                Declaration = new CodeType {
                                    Name = SerializationNamespaceName,
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", (string.Empty, new CodeUsing {
                                Name = "TimeOnly",
                                Declaration = new CodeType {
                                    Name = SerializationNamespaceName,
                                    IsExternal = true,
                                },
                            })},
        {"Guid", ("UUID", new CodeUsing {
                        Name = "UUID",
                        Declaration = new CodeType {
                            Name = "github.com/google/uuid",
                            IsExternal = true,
                        },
                    })},
        {KiotaBuilder.UntypedNodeName, (GoRefiner.UntypedNodeName, new CodeUsing {
                                Name = GoRefiner.UntypedNodeName,
                                Declaration = new CodeType {
                                    Name = SerializationNamespaceName,
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.Type != null)
        {
            if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
                currentProperty.Type.Name = "RequestAdapter";
            else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
            {
                currentProperty.Type.Name = "map[string]any";
                currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
            {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "map[string]string";
                if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            {
                currentProperty.DefaultValue = $"New{currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.Options))
            {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "RequestOption";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            }
            CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, types: currentProperty.Type);
        }
    }
    /// <summary>
    /// Go doesn't support the concept of an inner type, so we're writing them at the same level as the parent one. However that can result into conflicts with other existing models.
    /// This method will correct the type names to avoid conflicts.
    /// </summary>
    /// <param name="currentElement">The current element to start the renaming from.</param>
    private static void RenameInnerModelsToAppended(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentInnerClass &&
            currentInnerClass.IsOfKind(CodeClassKind.Model) &&
            currentInnerClass.Parent is CodeClass currentParentClass &&
            currentParentClass.IsOfKind(CodeClassKind.Model))
        {
            var oldName = currentInnerClass.Name;
            currentInnerClass.Name = $"{currentParentClass.Name.ToFirstCharacterUpperCase()}_{currentInnerClass.Name.ToFirstCharacterUpperCase()}";
            foreach (var property in currentParentClass.Properties.Where(x => x.Type.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                property.Type.Name = currentInnerClass.Name;
            foreach (var method in currentParentClass.Methods.Where(x => x.ReturnType.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                method.ReturnType.Name = currentInnerClass.Name;
            foreach (var parameter in currentParentClass.Methods.SelectMany(static x => x.Parameters).Where(x => x.Type.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                parameter.Type.Name = currentInnerClass.Name;
        }
        CrawlTree(currentElement, RenameInnerModelsToAppended);
    }

    private void NormalizeNamespaceNames(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace codeNamespace)
        {
            var clientNamespace = _configuration.ClientNamespaceName;
            var namespaceName = codeNamespace.Name;
            if (namespaceName.StartsWith(clientNamespace, StringComparison.OrdinalIgnoreCase) && !namespaceName.Equals(clientNamespace, StringComparison.OrdinalIgnoreCase))
            {
                var secondPart = namespaceName[clientNamespace.Length..]; // The rest of the name after the clientNamespace
                var withEmptyRemoved = string.Join('.', secondPart.Split('.', StringSplitOptions.RemoveEmptyEntries));
                var normalizedString = string.IsNullOrEmpty(withEmptyRemoved) switch
                {
                    true => string.Empty,
                    false => $".{withEmptyRemoved}"
                };
                codeNamespace.Name = $"{clientNamespace}{normalizedString.ToLowerInvariant()}";
            }
        }
        CrawlTree(currentElement, NormalizeNamespaceNames);
    }
}
