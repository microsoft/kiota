using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

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
    };


    public DartRefinerFromScratch(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();
            CorrectCommonNames(generatedCode);
            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                });

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

            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddInheritedTypeImport(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, false);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();

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

            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);


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
        else if (currentElement is CodeEnum e)
        {
            foreach (var option in e.Options)
            {
                if (!string.IsNullOrEmpty(option.Name) && Char.IsLower(option.Name[0]))
                {
                    if (string.IsNullOrEmpty(option.SerializationName))
                    {
                        option.SerializationName = option.Name;
                    }
                    option.Name = option.Name.ToFirstCharacterUpperCase();
                }
            }
        }

        CrawlTree(currentElement, element => CorrectCommonNames(element));
    }

    private static void CorrectMethodType(CodeMethod currentMethod)
    {

    }

    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        ArgumentNullException.ThrowIfNull(currentProperty);

        if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "List<RequestOption>()";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
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
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Map<String, Object?>";
            currentProperty.DefaultValue = "Map<String, Object?>()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "Map<String, Object>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "Map<String, Object>()";
        }
        currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
        // TODO KEES
        // CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }

    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void AddInheritedTypeImport(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
           currentClass.StartBlock is ClassDeclaration currentClassDeclaration &&
           currentClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentClassNamespace)
        {
            var inheritTypes = currentClassDeclaration.Inherits?.AllTypes ?? Enumerable.Empty<CodeType>();
            var usingsToAdd = inheritTypes
                            .SelectMany(static x => x.AllTypes.Select(static y => (type: y, ns: y.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())))
                            .Where(static x => x.ns != null)
                            .Select(static x => new CodeUsing { Name = x.ns!.Name, Declaration = x.type })
                            .Where(x => x.Declaration?.TypeDefinition != currentElement)
                            .ToArray();


            if (usingsToAdd.Length != 0)
                (currentClass.Parent is CodeClass parentClass ? parentClass : currentClass).AddUsing(usingsToAdd);
        }
        CrawlTree(currentElement, AddInheritedTypeImport);
    }
}
