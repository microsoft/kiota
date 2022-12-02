using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Refiners;
public class GoRefiner : CommonLanguageRefiner
{
    public GoRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        _configuration.NamespaceNameSeparator = "/";
        return Task.Run(() => {
            cancellationToken.ThrowIfCancellationRequested();
            FlattenNestedHierarchy(generatedCode);
            FlattenGoParamsFileNames(generatedCode);
            FlattenGoFileNames(generatedCode);
            AddInnerClasses(
                generatedCode,
                true,
                null,
            false,
            MergeOverLappedStrings);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                generatedCode,
                false,
                "ById");
            RenameCancellationParameter(generatedCode);
            RemoveDiscriminatorMappingsTargetingSubNamespaces(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceRequestBuilderPropertiesByMethods(
                generatedCode
            );
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore
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
                x => $"{x}_escaped",
                shouldReplaceCallback: x => x is not CodeProperty currentProp || 
                                            !(currentProp.Parent is CodeClass parentClass &&
                                            parentClass.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.ParameterSet) &&
                                            currentProp.Access == AccessModifier.Public)); // Go reserved keywords are all lowercase and public properties are uppercased when we don't provide accessors (models)
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
                new () { 
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.Custom,
                    CodePropertyKind.BackingStore }, 
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
                new (StringComparer.OrdinalIgnoreCase) {
                    "github.com/microsoft/kiota-serialization-json-go.JsonSerializationWriterFactory",
                    "github.com/microsoft/kiota-serialization-text-go.TextSerializationWriterFactory"});
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new (StringComparer.OrdinalIgnoreCase) {
                    "github.com/microsoft/kiota-serialization-json-go.JsonParseNodeFactory",
                    "github.com/microsoft/kiota-serialization-text-go.TextParseNodeFactory"});
            AddSerializationModulesImport(
                generatedCode,
                new[] {"github.com/microsoft/kiota-abstractions-go/serialization.SerializationWriterFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultSerializer"},
                new[] {"github.com/microsoft/kiota-abstractions-go/serialization.ParseNodeFactory", "github.com/microsoft/kiota-abstractions-go.RegisterDefaultDeserializer"});
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
        }, cancellationToken);
    }

    private static String MODELS_FOLDER = "models";
    private static String BUILDERS_FOLDER = "builders";
    
    private string MergeOverLappedStrings(string start, string end)
    {
        start = start.ToFirstCharacterUpperCase();
        end = end.ToFirstCharacterUpperCase();
        var endPattern = end.Substring(0, end.IndexOf("RequestBuilder",StringComparison.CurrentCultureIgnoreCase) + "RequestBuilder".Length);

        if (start.EndsWith(endPattern))
            return $"{start.Substring(0, start.IndexOf(endPattern))}{end}";
            
        return $"{start}{end}";
    }

    private static void CorrectTypes(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters) && currentElement.Parent is CodeClass parentClass)
        {
            var currentNamespace = currentMethod.GetImmediateParentOfType<CodeNamespace>();
            if (currentNamespace.Depth > 0)
            {
                var codeType = currentMethod.ReturnType;
                if (codeType is CodeType ct && !ct.Name.Equals(ct.TypeDefinition?.Name))
                {
                    ct.Name = ct.TypeDefinition?.Name;
                }
            }
        }
        CrawlTree(currentElement, CorrectTypes);
    }
    
    private CodeNamespace parentNames;
    private CodeNamespace findParentNameSpace(CodeElement currentElement)
    {
        if (currentElement == null) return null;
        if (parentNames != null) return parentNames;
        
        var currentNamespace = currentElement.GetImmediateParentOfType<CodeNamespace>();
        if (currentNamespace != null && _configuration.ClientNamespaceName.ToLower().Equals(currentNamespace.Name.ToLower()))
        {
            parentNames = currentNamespace;
        }

        return findParentNameSpace(currentElement.Parent);
    }

    private void FlattenNestedHierarchy(CodeElement currentElement) {
        // move all models and request builders nested to the top level domain
        if (currentElement is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model))
        {
            // if the parent is not the models namespace rename and move it
            var currentNamespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
            var parentNameSpace = findParentNameSpace(currentNamespace);

            var modelNameSpace = parentNameSpace.FindOrAddNamespace(_configuration.ClientNamespaceName + "." + MODELS_FOLDER);
            if (!modelNameSpace.Name.Equals(currentNamespace.Name) && !currentNamespace.IsChildOf(modelNameSpace))
            {
                // rename the nested class and move it to the models namespace
                var classNameList = getPathsName(codeClass, codeClass.Name);
                
                var newClassName = string.Join(String.Empty,classNameList);
                if (!codeClass.Name.ToLower().Equals(newClassName.ToLower()))
                {
                    currentNamespace.RemoveChildElement(codeClass);
                    codeClass.Name = newClassName;
                    codeClass.Parent = modelNameSpace;
                    modelNameSpace.AddClass(codeClass);
                }
            }
        }

        CrawlTree(currentElement, FlattenNestedHierarchy);
    }

    private void FlattenGoParamsFileNames(CodeElement currentElement)
    {
        if (currentElement is CodeProperty currentProp && currentElement.Parent is CodeClass parentClass && parentClass.IsOfKind(CodeClassKind.RequestConfiguration) && currentProp.IsOfKind(CodePropertyKind.QueryParameters))
        {
            var nameList = getPathsName(parentClass, currentProp.Type.Name.ToFirstCharacterUpperCase());
            var newTypeName = string.Join(String.Empty,nameList);
                        
            var type = currentProp.Type;
            type.Name = newTypeName;
        }

        if (currentElement is CodeMethod codeMethod && codeMethod.IsOfKind(CodeMethodKind.RequestGenerator, CodeMethodKind.RequestExecutor))
        {
            foreach (var param in codeMethod.Parameters){
                if (param.IsOfKind(CodeParameterKind.RequestConfiguration)){
                    var newTypeName = string.Join(String.Empty,getPathsName(param, param.Type.Name.ToFirstCharacterUpperCase()));
                    param.Type.Name = newTypeName;
                    
                    foreach (var ct  in param.Type.AllTypes)
                        if(!newTypeName.EndsWith(ct.TypeDefinition.Name.ToFirstCharacterUpperCase()))
                            ct.TypeDefinition.Name = newTypeName;
                }
            }
            
        }

        CrawlTree(currentElement, FlattenGoParamsFileNames);
    }

    private List<string> getPathsName(CodeElement codeClass, string fileName, bool removeDuplicate = true)
    {
        // update the code class name to include the entire path
        var currentNamespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
        var namespacePathSegments = new List<string>(currentNamespace.Name
            .Replace(_configuration.ClientNamespaceName, string.Empty)
            .TrimStart('.')
            .Split('.'));
        // add the namespace to the code class Name
        namespacePathSegments = namespacePathSegments.Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x.ToFirstCharacterUpperCase().Trim())
            .ToList();
            
        var classNameList = new List<string>(namespacePathSegments);
        if (classNameList.Count > 0)
        {
            // check if the last element contains a name and remove it
            var lastElement = classNameList.Last();
            if (removeDuplicate && fileName.ToFirstCharacterUpperCase().Contains(lastElement))
            {
                classNameList.RemoveAt(classNameList.Count - 1);
            }
        }
        classNameList.Add(fileName.ToFirstCharacterUpperCase());
        return classNameList;
    }

    private static CodeNamespace findParentAsLevel(CodeNamespace rootNameSpace, CodeNamespace currentNameSpace, int childLevel)
    {
        CodeNamespace checkSpace = currentNameSpace;
        List<CodeNamespace> position = new List<CodeNamespace>();
        position.Add(currentNameSpace);
        while (checkSpace != null && !checkSpace.IsChildOf(rootNameSpace, true))
        {
            var foundNameSpace = checkSpace.GetImmediateParentOfType<CodeNamespace>(checkSpace.Parent);
            checkSpace = foundNameSpace;
            if (checkSpace != null)
            {
                position.Add(checkSpace);
            }
        }
        return position[^childLevel];
    }
    
    private void FlattenGoFileNames(CodeElement currentElement) {
        
        // add the namespace to the name of the code element and the file name
        if (currentElement is CodeClass codeClass && codeClass.Parent is not null && !codeClass.IsOfKind(CodeClassKind.Model) && codeClass.Parent is CodeNamespace currentNamespace)
        {
            var rootNameSpace = findParentNameSpace(codeClass.Parent);
            if (!rootNameSpace.Name.Equals(currentNamespace.Name) && !currentNamespace.IsChildOf(rootNameSpace, true))
            {
                var classNameList = getPathsName(codeClass, codeClass.Name.ToFirstCharacterUpperCase());
                var newClassName = string.Join(String.Empty,classNameList);
                
                var nextNameSpace = findParentAsLevel(rootNameSpace, currentNamespace, 1);
                currentNamespace.RemoveChildElement(codeClass);
                codeClass.Name = newClassName;
                codeClass.Parent = nextNameSpace;
                nextNameSpace.AddClass(codeClass);
            }
        }

        CrawlTree(currentElement, FlattenGoFileNames);
    }
    
    protected static void RenameCancellationParameter(CodeElement currentElement){
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) && currentMethod.Parameters.OfKind(CodeParameterKind.Cancellation) is CodeParameter parameter)
        {
            parameter.Name = ContextParameterName;
            parameter.Description = ContextVarDescription;
            parameter.Kind = CodeParameterKind.Cancellation;
            parameter.Optional = false;
            parameter.Type.Name = conventions.ContextVarTypeName;
            parameter.Type.IsNullable = false;
        }
        CrawlTree(currentElement, RenameCancellationParameter);
    }
    private const string ContextParameterName = "ctx";
    private const string ContextVarDescription = "Pass a context parameter to the request";
    private static void AddContextParameterToGeneratorMethods(CodeElement currentElement) {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.RequestGenerator) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.Cancellation) is null)
            currentMethod.AddParameter(new CodeParameter {
                Name = ContextParameterName,
                Type = new CodeType {
                    Name = conventions.ContextVarTypeName,
                    IsNullable = false,
                },
                Kind = CodeParameterKind.Cancellation,
                Optional = false,
                Description = ContextVarDescription,
            });
        CrawlTree(currentElement, AddContextParameterToGeneratorMethods);
    }

    private static void RemoveModelPropertiesThatDependOnSubNamespaces(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass && 
            currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Parent is CodeNamespace currentNamespace) {
            var propertiesToRemove = currentClass.Properties
                                                    .Where(x => x.IsOfKind(CodePropertyKind.Custom) &&
                                                                x.Type is CodeType pType &&
                                                                !pType.IsExternal &&
                                                                pType.TypeDefinition != null &&
                                                                currentNamespace.IsParentOf(pType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>()))
                                                    .ToArray();
            if(propertiesToRemove.Any()) {
                currentClass.RemoveChildElement(propertiesToRemove);
                var propertiesToRemoveHashSet = propertiesToRemove.ToHashSet();
                var methodsToRemove = currentClass.Methods
                                                    .Where(x => x.IsAccessor &&
                                                            propertiesToRemoveHashSet.Contains(x.AccessedProperty))
                                                    .ToArray();
                currentClass.RemoveChildElement(methodsToRemove);
            }
        }
        CrawlTree(currentElement, RemoveModelPropertiesThatDependOnSubNamespaces);
    }
    private static CodeNamespace FindFirstModelSubnamepaceWithClasses(CodeNamespace currentNamespace) {
        if(currentNamespace != null)
        {
            if(currentNamespace.Classes.Any()) return currentNamespace;
            foreach (var subNS in currentNamespace.Namespaces)
            {
                var result = FindFirstModelSubnamepaceWithClasses(subNS);
                if (result != null) return result;
            }
        }
        return null;
    }
    private static CodeNamespace FindRootModelsNamespace(CodeNamespace currentNamespace) {
        if(currentNamespace != null)
        {
            if(!string.IsNullOrEmpty(currentNamespace.Name) &&
               currentNamespace.Name.EndsWith("Models", StringComparison.OrdinalIgnoreCase))
                return currentNamespace;
            foreach(var subNS in currentNamespace.Namespaces)
            {
                var result = FindRootModelsNamespace(subNS);
                if(result != null)
                    return result;
            }
        }
        return null;
    }
    private static void ReplaceRequestBuilderPropertiesByMethods(CodeElement currentElement) {
        if(currentElement is CodeProperty currentProperty &&
            currentProperty.IsOfKind(CodePropertyKind.RequestBuilder) &&
            currentElement.Parent is CodeClass parentClass) {
                parentClass.RemoveChildElement(currentProperty);
                currentProperty.Type.IsNullable = false;
                parentClass.AddMethod(new CodeMethod {
                    Name = currentProperty.Name,
                    ReturnType = currentProperty.Type,
                    Access = AccessModifier.Public,
                    Description = currentProperty.Description,
                    IsAsync = false,
                    Kind = CodeMethodKind.RequestBuilderBackwardCompatibility,
                });
            }
        CrawlTree(currentElement, ReplaceRequestBuilderPropertiesByMethods);
    }
    private static void AddErrorImportForEnums(CodeElement currentElement) {
        if(currentElement is CodeEnum currentEnum) {
            currentEnum.AddUsing(new CodeUsing {
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
            "github.com/microsoft/kiota-abstractions-go", "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "github.com/microsoft/kiota-abstractions-go", "RequestInformation", "HttpMethod", "RequestOption"),
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
        /*new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer)
            && method.Parent is CodeClass codeClass && codeClass.GetPropertiesOfKind(CodePropertyKind.Custom).Any(static x => !x.ExistsInBaseType),
            "github.com/microsoft/kiota-abstractions-go", ""),*/
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
    };//TODO add backing store types once we have them defined
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        var parentClass = currentMethod.Parent as CodeClass;
        if(currentMethod.IsOfKind(CodeMethodKind.RequestGenerator)) {
            currentMethod.ReturnType.IsNullable = true;
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
        else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
            currentMethod.ReturnType.Name = $"map[string]func({conventions.SerializationHash}.ParseNode)(error)";
            currentMethod.Name = "getFieldDeserializers";
        } else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
            var rawUrlParam = currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl);
            if(rawUrlParam != null)
                rawUrlParam.Type.IsNullable = false;
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                .Where(static x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        } else if(currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.Factory)) {
            currentMethod.ReturnType.IsNullable = true;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam) {
                parseNodeParam.Type.IsNullable = false;
                parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
            }

            if(currentMethod.IsOfKind(CodeMethodKind.Factory))
                currentMethod.ReturnType = new CodeType { Name = "Parsable", IsNullable = false, IsExternal = true };
        }
        CorrectDateTypes(parentClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType})
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
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
        {"DateOnly", (null, new CodeUsing {
                                Name = "DateOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", (null, new CodeUsing {
                                Name = "TimeOnly",
                                Declaration = new CodeType {
                                    Name = "github.com/microsoft/kiota-abstractions-go/serialization",
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if (currentProperty.Type != null) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
                currentProperty.Type.Name = "RequestAdapter";
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "map[string]interface{}";
                currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "map[string]string";
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.Headers)) {
                currentProperty.Type.Name = "map[string]string";
                currentProperty.DefaultValue = $"make({currentProperty.Type.Name})";
            } else if(currentProperty.IsOfKind(CodePropertyKind.Options)) {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "RequestOption";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            }
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        }
    }
    /// <summary>
    /// Go doesn't support the concept of an inner type, so we're writing them at the same level as the parent one. However that can result into conflicts with other existing models.
    /// This method will correct the type names to avoid conflicts.
    /// </summary>
    /// <param name="currentElement">The current element to start the renaming from.</param>
    private static void RenameInnerModelsToAppended(CodeElement currentElement) {
        if(currentElement is CodeClass currentInnerClass &&
            currentInnerClass.IsOfKind(CodeClassKind.Model) &&
            currentInnerClass.Parent is CodeClass currentParentClass &&
            currentParentClass.IsOfKind(CodeClassKind.Model))
        {
            var oldName = currentInnerClass.Name;
            currentInnerClass.Name = $"{currentParentClass.Name.ToFirstCharacterUpperCase()}_{currentInnerClass.Name.ToFirstCharacterUpperCase()}";
            foreach(var property in currentParentClass.Properties.Where(x => x.Type.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                property.Type.Name = currentInnerClass.Name;
            foreach(var method in currentParentClass.Methods.Where(x => x.ReturnType.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                method.ReturnType.Name = currentInnerClass.Name;
            foreach(var parameter in currentParentClass.Methods.SelectMany(static x => x.Parameters).Where(x => x.Type.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                parameter.Type.Name = currentInnerClass.Name;
        }
        CrawlTree(currentElement, RenameInnerModelsToAppended);
    }
}
