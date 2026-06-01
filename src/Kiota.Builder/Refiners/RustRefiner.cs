using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class RustRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    private const string AbstractionsNamespaceName = "kiota_abstractions";
    private const string SerializationNamespaceName = "kiota_abstractions::serialization";

    public RustRefiner(GenerationConfiguration configuration) : base(configuration) { }

    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _configuration.NamespaceNameSeparator = "::";
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
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                },
                accessModifier: AccessModifier.Public);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                false,
                static x => $"by_{x.ToSnakeCase()}",
                static x => x.ToSnakeCase(),
                GenerationLanguage.Rust);
            cancellationToken.ThrowIfCancellationRequested();

            AddInnerClasses(generatedCode, true, string.Empty, false);
            cancellationToken.ThrowIfCancellationRequested();

            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType { Name = AbstractionsNamespaceName, IsExternal = true }
                },
                new CodeType { Name = "DefaultQueryParameters", IsExternal = true });
            RemoveCancellationParameter(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();

            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                static s => s.ToSnakeCase(),
                true,
                string.Empty,
                string.Empty,
                "is_composed_type"
            );
            PromoteComposedTypesToNamespace(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();

            ReplaceReservedNames(
                generatedCode,
                new RustReservedNamesProvider(),
                x => $"r#{x}",
                shouldReplaceCallback: static x => x is not CodeEnumOption && x is not CodeEnum);
            ReplaceReservedExceptionPropertyNames(
                generatedCode,
                new RustExceptionsReservedNamesProvider(),
                static x => $"{x}_prop");

            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            cancellationToken.ThrowIfCancellationRequested();

            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            DisableActionOf(generatedCode, CodeParameterKind.RequestConfiguration);

            AddGetterAndSetterMethods(generatedCode,
                new()
                {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore,
                },
                static (_, s) => s.ToSnakeCase(),
                _configuration.UsesBackingStore,
                false, "get_", "set_");
            AddConstructorsForDefaultValues(generatedCode, true, true);
            MakeModelPropertiesNullable(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();

            var defaultConfiguration = new GenerationConfiguration();
            ReplaceDefaultSerializationModules(generatedCode, defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "kiota_serialization_json::JsonSerializationWriterFactory",
                    "kiota_serialization_text::TextSerializationWriterFactory",
                    "kiota_serialization_form::FormSerializationWriterFactory",
                });
            ReplaceDefaultDeserializationModules(generatedCode, defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "kiota_serialization_json::JsonParseNodeFactory",
                    "kiota_serialization_text::TextParseNodeFactory",
                    "kiota_serialization_form::FormParseNodeFactory",
                });
            AddParentClassToErrorClasses(generatedCode, "ApiError", AbstractionsNamespaceName);
            AddDiscriminatorMappingsUsingsToParentClasses(generatedCode, "ParseNode", true);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            cancellationToken.ThrowIfCancellationRequested();

            ReplacePropertyNames(generatedCode,
                new()
                {
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                },
                static s => s.ToSnakeCase());
            AddPrimaryErrorMessage(generatedCode, "error_message",
                () => new CodeType { Name = "String", IsNullable = false, IsExternal = true });
            NormalizeEnumNames(generatedCode);
        }, cancellationToken);
    }

    /// Promotes model-kind classes (composed type wrappers) out of request builder classes
    /// and into the request builder's parent namespace so they become separate .rs files.
    private static void PromoteComposedTypesToNamespace(CodeElement currentElement)
    {
        if (currentElement is CodeClass parentClass && parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var parentNamespace = parentClass.GetImmediateParentOfType<CodeNamespace>();
            if (parentNamespace != null)
            {
                var toPromote = parentClass.InnerClasses
                    .Where(static c => !c.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.RequestConfiguration, CodeClassKind.ParameterSet))
                    .ToList();
                foreach (var inner in toPromote)
                {
                    parentClass.RemoveChildElementByName(inner.Name);
                    inner.Parent = parentNamespace;
                    parentNamespace.AddClass(inner);
                }
            }
        }
        CrawlTree(currentElement, PromoteComposedTypesToNamespace);
    }

    /// Normalize enum names: "Order_status" -> "OrderStatus"
    private static void NormalizeEnumNames(CodeElement currentElement)
    {
        if (currentElement is CodeEnum codeEnum)
        {
            var newName = string.Join("", codeEnum.Name.Split('_').Select(static s => s.ToFirstCharacterUpperCase()));
            if (!newName.Equals(codeEnum.Name, StringComparison.Ordinal))
                codeEnum.Name = newName;
        }
        CrawlTree(currentElement, NormalizeEnumNames);
    }

    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators =
    [
        new(static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "RequestAdapter"),
        new(static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "RequestInformation", "HttpMethod", "RequestOption"),
        new(static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            SerializationNamespaceName, "SerializationWriter"),
        new(static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            SerializationNamespaceName, "ParseNode", "Parsable"),
        new(static x => x is CodeClass cls && cls.IsOfKind(CodeClassKind.Model),
            SerializationNamespaceName, "Parsable"),
        new(static x => x is CodeClass cls && cls.IsOfKind(CodeClassKind.Model) &&
            cls.Properties.Any(static p => p.IsOfKind(CodePropertyKind.AdditionalData)),
            SerializationNamespaceName, "AdditionalDataHolder"),
        new(static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
    ];

    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
        {
            currentMethod.Parameters
                .Where(static x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]);
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "FieldDeserializers";
            currentMethod.Name = "get_field_deserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Factory))
        {
            currentMethod.Parameters
                .Where(static x => x.IsOfKind(CodeParameterKind.ParseNode) && x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]);
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor))
        {
            currentMethod.Parameters
                .Where(static x => x.IsOfKind(CodeParameterKind.RequestAdapter) && x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]);

            if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathsParam)
            {
                pathsParam.Type.Name = "HashMap<String, String>";
                pathsParam.Type.IsNullable = true;
            }
        }

        currentMethod.Parameters
            .ToList()
            .ForEach(static x => x.Name = x.Name.ToFirstCharacterLowerCase());
    }

    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            if (currentProperty.Type.Name.StartsWith('I'))
                currentProperty.Type.Name = currentProperty.Type.Name[1..];
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "HashMap<String, serde_json::Value>";
            currentProperty.DefaultValue = "HashMap::new()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "HashMap<String, String>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "HashMap::new()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
        {
            currentProperty.DefaultValue = "RequestHeaders::new()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.Options))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Vec<Box<dyn RequestOption>>";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
        {
            if (currentProperty.Type.Name.StartsWith('I'))
                currentProperty.Type.Name = currentProperty.Type.Name[1..];
        }
    }

    private void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
        block.ReplaceImplementByName(KiotaBuilder.BackedModelInterface, "BackedModel");
    }
}
