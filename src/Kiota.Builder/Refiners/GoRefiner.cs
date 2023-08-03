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
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        _configuration.NamespaceNameSeparator = "/";
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                static x => x.ToFirstCharacterLowerCase());
            FlattenNestedHierarchy(generatedCode);
            FlattenGoParamsFileNames(generatedCode);
            FlattenGoFileNames(generatedCode);
            AddInnerClasses(
                generatedCode,
                true,
                string.Empty,
                false,
                MergeOverLappedStrings);
            RenameCancellationParameter(generatedCode);
            RemoveDiscriminatorMappingsTargetingSubNamespaces(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceRequestBuilderPropertiesByMethods(
                generatedCode
            );
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                true,
                string.Empty,
                string.Empty,
                "GetIsComposedType"
            );
            AddRawUrlConstructorOverload(
                generatedCode
            );
            cancellationToken.ThrowIfCancellationRequested();
            RemoveModelPropertiesThatDependOnSubNamespaces(
                generatedCode
            );
            ReplaceReservedNames(
                generatedCode,
                new GoReservedNamesProvider(),
                x => $"{x}Escaped",
                shouldReplaceCallback: x => x is not CodeProperty currentProp ||
                                            !(currentProp.Parent is CodeClass parentClass &&
                                            parentClass.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.ParameterSet) &&
                                            currentProp.Access == AccessModifier.Public)); // Go reserved keywords are all lowercase and public properties are uppercased when we don't provide accessors (models)
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
            AddErrorImportForEnums(
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
                new[] { "github.com/microsoft/kiota-abstractions-go/serialization.SerializationWriterFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultSerializer" },
                new[] { "github.com/microsoft/kiota-abstractions-go/serialization.ParseNodeFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultDeserializer" });
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
            CopyModelClassesAsInterfaces(
                generatedCode,
                x => $"{x.Name}able"
            );
            RemoveHandlerFromRequestBuilder(generatedCode);
            AddContextParameterToGeneratorMethods(generatedCode);
            CorrectTypes(generatedCode);
            CorrectCoreTypesForBackingStore(generatedCode, $"{conventions.StoreHash}.BackingStoreFactoryInstance()", false);
            CorrectBackingStoreTypes(generatedCode);
            GenerateCodeFiles(generatedCode);
        }, cancellationToken);
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
                ?.FindNamespaceByName($"{_configuration.ClientNamespaceName}.models");

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
                    type.Name = interfaceName;
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
            var modelNameSpace = parentNameSpace.FindNamespaceByName($"{_configuration.ClientNamespaceName}.models");
            var packageRootNameSpace = findNameSpaceAtLevel(parentNameSpace, currentNamespace, 1);
            if (!packageRootNameSpace.Name.Equals(currentNamespace.Name, StringComparison.Ordinal) && modelNameSpace != null && !currentNamespace.IsChildOf(modelNameSpace))
            {
                var classNameList = getPathsName(codeClass, codeClass.Name);
                var newClassName = string.Join(string.Empty, classNameList.Count > 1 ? classNameList.Skip(1) : classNameList);

                currentNamespace.RemoveChildElement(codeClass);
                codeClass.Name = newClassName;
                codeClass.Parent = packageRootNameSpace;
                packageRootNameSpace.AddClass(codeClass);
            }
        }

        CrawlTree(currentElement, FlattenNestedHierarchy);
    }

    private void FlattenGoParamsFileNames(CodeElement currentElement)
    {
        if (currentElement is CodeProperty currentProp
            && currentElement.Parent is CodeClass parentClass
            && parentClass.IsOfKind(CodeClassKind.RequestConfiguration)
            && currentProp.IsOfKind(CodePropertyKind.QueryParameters))
        {
            var nameList = getPathsName(parentClass, currentProp.Type.Name.ToFirstCharacterUpperCase());
            var newTypeName = string.Join(string.Empty, nameList.Count > 1 ? nameList.Skip(1) : nameList);

            var type = currentProp.Type;
            type.Name = newTypeName;
        }

        if (currentElement is CodeMethod codeMethod
            && codeMethod.IsOfKind(CodeMethodKind.RequestGenerator, CodeMethodKind.RequestExecutor))
        {
            foreach (var param in codeMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.RequestConfiguration)))
            {
                var nameList = getPathsName(param, param.Type.Name.ToFirstCharacterUpperCase());
                var newTypeName = string.Join(string.Empty, nameList.Count > 1 ? nameList.Skip(1) : nameList);
                param.Type.Name = newTypeName;

                foreach (var typeDef in param.Type.AllTypes.Select(static x => x.TypeDefinition).Where(x => !string.IsNullOrEmpty(x?.Name) && !newTypeName.EndsWith(x.Name, StringComparison.OrdinalIgnoreCase)))
                    typeDef!.Name = newTypeName;
            }

        }

        CrawlTree(currentElement, FlattenGoParamsFileNames);
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

    private void FlattenGoFileNames(CodeElement currentElement)
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

        CrawlTree(currentElement, FlattenGoFileNames);
    }

    protected static void RenameCancellationParameter(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) && currentMethod.Parameters.OfKind(CodeParameterKind.Cancellation) is CodeParameter parameter)
        {
            parameter.Name = ContextParameterName;
            parameter.Documentation.Description = ContextVarDescription;
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
                    Description = ContextVarDescription,
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
            if (propertiesToRemove.Any())
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
    private static void AddErrorImportForEnums(CodeElement currentElement)
    {
        if (currentElement is CodeEnum currentEnum)
        {
            currentEnum.AddUsing(new CodeUsing
            {
                Name = "errors",
            });
        }
        CrawlTree(currentElement, AddErrorImportForEnums);
    }
    private static readonly GoConventionService conventions = new();
    private static readonly HashSet<string> typeToSkipStrConv = new(StringComparer.OrdinalIgnoreCase) {
        "DateTimeOffset",
        "Duration",
        "TimeOnly",
        "DateOnly",
        "string"
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
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "github.com/microsoft/kiota-abstractions-go/serialization", "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            "github.com/microsoft/kiota-abstractions-go/serialization", "ParseNode", "Parsable"),
        new (static x => x is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model),
            "github.com/microsoft/kiota-abstractions-go/serialization", "Parsable"),
        new (static x => x is CodeMethod method &&
                         method.IsOfKind(CodeMethodKind.RequestGenerator) &&
                         method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.RequestBody) &&
                                                    x.Type.IsCollection &&
                                                    x.Type is CodeType pType &&
                                                    (pType.TypeDefinition is CodeClass ||
                                                     pType.TypeDefinition is CodeInterface)),
            "github.com/microsoft/kiota-abstractions-go/serialization", "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) &&
                                            (@class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)) ||
                                            @class.StartBlock.Implements.Any(x => KiotaBuilder.AdditionalHolderInterface.Equals(x.Name, StringComparison.OrdinalIgnoreCase))),
            "github.com/microsoft/kiota-abstractions-go/serialization", "AdditionalDataHolder"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeUnionType unionType && unionType.Types.Any(static y => !y.IsExternal) && unionType.DiscriminatorInformation.HasBasicDiscriminatorInformation,
            "strings", "EqualFold"),
        new (static x => x is CodeMethod method && (method.IsOfKind(CodeMethodKind.RequestExecutor) || method.IsOfKind(CodeMethodKind.RequestGenerator)), "context","*context"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal) && intersectionType.DiscriminatorInformation.HasBasicDiscriminatorInformation,
            "github.com/microsoft/kiota-abstractions-go/serialization", "MergeDeserializersForIntersectionWrapper"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore), "github.com/microsoft/kiota-abstractions-go/store","BackingStore"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                         method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "github.com/microsoft/kiota-abstractions-go/store", "BackingStoreFactory"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
    };
    private const string MultipartBodyClassName = "MultipartBody";
    private const string AbstractionsNamespaceName = "github.com/microsoft/kiota-abstractions-go";

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
                .Where(static x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
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
        CorrectCoreTypes(parentClass, DateTypesReplacements, currentMethod.Parameters
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
                                            Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", (string.Empty, new CodeUsing {
                                Name = "DateOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", (string.Empty, new CodeUsing {
                                Name = "TimeOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
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
            CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
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
}
