using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift;
public class SwiftConventionService : CommonLanguageConventionService
{
    public SwiftConventionService(string clientNamespaceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientNamespaceName);
        this.clientNamespaceName = clientNamespaceName;
    }
    // TODO (Swift) reevaluate entire file for Swift
    public override string StreamTypeName => "stream";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => "/// ";
    public static readonly char NullableMarker = '?';
    public static string NullableMarkerAsString => "?";
    public override string ParseNodeInterfaceName => "ParseNode";
    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        if (!string.IsNullOrEmpty(description))
            writer.WriteLine($"{DocCommentPrefix}<summary>{description}</summary>");
    }
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "internal",
            _ => "private",
        };
    }
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, string? prefix = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
        {
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(static x => x.Name.ToFirstCharacterLowerCase()))}";
            var urlTplRef = urlTemplateVarName ?? pathParametersProp.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{prefix}new {returnType}({urlTplRef}, {requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix});");
        }
    }
    public override string TempDictionaryVarName => "urlTplParams";
#pragma warning restore CA1822 // Method should be static
    private readonly string clientNamespaceName;
    private string GetNamesInUseByNamespaceSegments(CodeNamespace typeNS, CodeElement currentElement)
    {
        var currentNS = currentElement.GetImmediateParentOfType<CodeNamespace>();
        var diffResult = currentNS.GetDifferential(typeNS, clientNamespaceName);
        return diffResult.State switch
        {
            NamespaceDifferentialTrackerState.Same => string.Empty,
            NamespaceDifferentialTrackerState.Downwards => $"{string.Join('.', diffResult.DownwardsSegments)}.",
            NamespaceDifferentialTrackerState.Upwards => string.Empty, //TODO
            NamespaceDifferentialTrackerState.UpwardsAndThenDownwards => $"{typeNS.Name}.",
            _ => throw new NotImplementedException(),
        };
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"Swift does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var typeName = TranslateTypeAndAvoidUsingNamespaceSegmentNames(currentType, targetElement);
            var nullableSuffix = code.IsNullable ? NullableMarkerAsString : string.Empty;
            var collectionPrefix = currentType.IsCollection && includeCollectionInformation ? "[" : string.Empty;
            var collectionSuffix = currentType.IsCollection && includeCollectionInformation ? $"]{nullableSuffix}" : string.Empty;
            if (currentType.IsCollection && !string.IsNullOrEmpty(nullableSuffix))
                nullableSuffix = string.Empty;

            if (currentType.ActionOf)
                return $"({collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>) -> Void";
            return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }
    private string TranslateTypeAndAvoidUsingNamespaceSegmentNames(CodeType currentType, CodeElement targetElement)
    {
        var typeName = TranslateType(currentType);
        if (currentType.TypeDefinition != null)
            return $"{GetNamesInUseByNamespaceSegments(currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>(), targetElement)}{typeName}";
        return typeName;
    }
    public override string TranslateType(CodeType type)
    {
        return type.Name switch
        {
            "integer" => "Int32",
            "boolean" => "Bool",
            "float" => "Float32",
            "long" => "Int64",
            "double" or "decimal" => "Float64",
            "guid" => "UUID",
            "void" or "uint8" or "int8" or "int32" or "int64" or "float32" or "float64" or "string" => type.Name.ToFirstCharacterUpperCase(),
            "binary" => "[UInt8]",
            "DateTimeOffset" => "Date", // TODO
            _ => type.Name?.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        var parameterType = GetTypeString(parameter.Type, targetElement);
        var defaultValue = parameter switch
        {
            _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
            _ when parameter.Optional => " = default", // TODO (Swift) reevaluate
            _ => string.Empty,
        };
        return $"{parameter.Name.ToFirstCharacterLowerCase()} : {parameterType}{defaultValue}";
    }
}
