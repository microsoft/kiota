using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Php;

public class PhpConventionService : CommonLanguageConventionService
{
    public override string TempDictionaryVarName => "urlTplParams";

    private static readonly CodeUsingDeclarationNameComparer _usingDeclarationNameComparer = new();

    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private"
        };
    }
    public override string StreamTypeName => "StreamInterface";

    public override string VoidTypeName => "void";

    public override string DocCommentPrefix => " * ";

    private static string PathParametersPropertyName => "$pathParameters";

    private static string RequestAdapterPropertyName => "$requestAdapter";

    public override string ParseNodeInterfaceName => "ParseNode";

    public const string DocCommentStart = "/**";

    public const string DocCommentEnd = "*/";
    internal HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase) { "string", "boolean", "integer", "float", "date", "datetime", "time", "dateinterval", "int", "double", "decimal", "bool" };

    internal HashSet<string> ScalarTypes = new(StringComparer.Ordinal) { "string", "int", "float", "double", "bool", "array" };

    internal readonly HashSet<string> CustomTypes = new(StringComparer.OrdinalIgnoreCase) { "Date", "DateTime", "StreamInterface", "Byte", "Time" };
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"PHP does not support union types, the union type {code.Name} should have been filtered out by the refiner.");
        if (code is CodeType currentType)
        {
            if (includeCollectionInformation && code.IsCollection)
            {
                return "array";
            }
            var typeName = TranslateType(currentType);
            if (!currentType.IsExternal && IsSymbolDuplicated(typeName, targetElement) && currentType.TypeDefinition is not null)
            {
                return $"\\{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name.ReplaceDotsWithSlashInNamespaces()}\\{typeName.ToFirstCharacterUpperCase()}";
            }
        }
        return TranslateType(code);
    }

    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Name.ToLowerInvariant() switch
        {
            "boolean" => "bool",
            "double" => "float",
            "decimal" or "byte" or "guid" => "string",
            "integer" or "int32" or "int64" or "sbyte" => "int",
            "object" or "string" or "array" or "float" or "void" => type.Name.ToLowerInvariant(),
            "binary" or "base64" or "base64url" => "StreamInterface",
            _ => type.Name.ToFirstCharacterUpperCase()
        };
    }

    public string GetParameterName(CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Kind switch
        {
            CodeParameterKind.RequestConfiguration => "$requestConfiguration",
            CodeParameterKind.BackingStore => "$backingStore",
            CodeParameterKind.PathParameters => "$pathParametersOrRawUrl",
            CodeParameterKind.RequestAdapter => RequestAdapterPropertyName,
            CodeParameterKind.RequestBody => "$body",
            CodeParameterKind.RawUrl => "$rawUrl",
            CodeParameterKind.Serializer => "$writer",
            CodeParameterKind.SetterValue => "$value",
            _ => $"${parameter.Name.ToFirstCharacterLowerCase()}"
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var typeString = GetTypeString(parameter.Type, parameter);
        var parameterSuffix = parameter.Kind switch
        {
            CodeParameterKind.RequestAdapter => $"RequestAdapter {GetParameterName(parameter)}",
            CodeParameterKind.PathParameters => GetParameterName(parameter),
            CodeParameterKind.Serializer => $"SerializationWriter {GetParameterName(parameter)}",
            _ => $"{typeString} {GetParameterName(parameter)}"
        };
        var optional = parameter.Optional ? "?" : "";
        var qualified = (parameter.Optional
            && targetElement is CodeMethod methodTarget
            && !methodTarget.IsOfKind(CodeMethodKind.Setter)) ? " = null" : "";
        return $"{optional}{parameterSuffix}{qualified}";
    }
    public string GetParameterDocNullable(CodeParameter parameter, CodeElement codeElement)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var parameterSignature = GetParameterSignature(parameter, codeElement).Trim().Split(' ');
        if (parameter.IsOfKind(CodeParameterKind.PathParameters))
        {
            return $"array<string, mixed>|string{(parameter.Optional ? "|null" : string.Empty)} {parameterSignature[0]}";
        }
        if (parameter.Type.IsCollection)
        {
            return $"{GetCollectionDocString(parameter)} {parameterSignature[1]}";
        }
        return parameter.Optional ? $"{parameterSignature[0].Trim('?')}|null {parameterSignature[1]}" : string.Join(' ', parameterSignature);
    }
    private string GetCollectionDocString(CodeParameter codeParameter)
    {
        var doc = codeParameter.Kind switch
        {
            CodeParameterKind.PathParameters => $"array<string, mixed>|string",
            CodeParameterKind.Headers => "array<string, array<string>|string>",
            CodeParameterKind.Options => "array<RequestOption>",
            _ => $"array<{TranslateType(codeParameter.Type)}>"
        };
        return codeParameter.Optional ? $"{doc}|null" : doc;
    }

    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription.Replace("\\", "/", StringComparison.OrdinalIgnoreCase).Replace("*/", string.Empty, StringComparison.OrdinalIgnoreCase);
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Documentation.DescriptionAvailable) return false;
        if (element is not CodeElement codeElement) return false;

        var description = element.Documentation.GetDescription(type => GetTypeString(type, codeElement), normalizationFunc: RemoveInvalidDescriptionCharacters);

        writer.WriteLine(DocCommentStart);
        writer.WriteLine($"{DocCommentPrefix}{description}");
        writer.WriteLine(DocCommentEnd);

        return true;
    }

    public void WriteLongDescription(IDocumentedElement element, LanguageWriter writer, IEnumerable<string>? additionalRemarks = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element.Documentation is not { } documentation) return;
        if (element is not CodeElement codeElement) return;
        additionalRemarks ??= [];

        var enumerableArray = additionalRemarks as string[] ?? additionalRemarks.ToArray();
        if (documentation.DescriptionAvailable || documentation.ExternalDocumentationAvailable ||
            enumerableArray.Length != 0)
        {
            writer.WriteLine(DocCommentStart);
            if (documentation.DescriptionAvailable)
            {
                var description = element.Documentation.GetDescription(type => GetTypeString(type, codeElement), normalizationFunc: RemoveInvalidDescriptionCharacters);
                writer.WriteLine($"{DocCommentPrefix}{description}");
            }
            foreach (var additionalRemark in enumerableArray.Where(static x => !string.IsNullOrEmpty(x)))
                writer.WriteLine($"{DocCommentPrefix}{additionalRemark}");

            if (documentation.ExternalDocumentationAvailable)
                writer.WriteLine($"{DocCommentPrefix}@link {documentation.DocumentationLink} {documentation.DocumentationLabel}");
            writer.WriteLine(DocCommentEnd);
        }

    }

    public void AddRequestBuilderBody(string returnType, LanguageWriter writer, string? suffix = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var joined = string.Empty;
        if (pathParameters?.ToList() is { } codeParameters && codeParameters.Count != 0)
        {
            joined = $", {string.Join(", ", codeParameters.Select(static x => $"${x.Name.ToFirstCharacterLowerCase()}"))}";
        }

        writer.WriteLines($"return new {returnType}($this->{RemoveDollarSignFromPropertyName(PathParametersPropertyName)}{suffix}, $this->{RemoveDollarSignFromPropertyName(RequestAdapterPropertyName)}{joined});");
    }

    private static string RemoveDollarSignFromPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || propertyName.Length < 2)
        {
            throw new ArgumentException(nameof(propertyName) + " must not be null and have at least 2 characters.");
        }

        return propertyName[1..];
    }

    public void WritePhpDocumentStart(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLines("<?php", string.Empty);
    }
    public void WriteNamespaceAndImports(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        bool hasUse = false;
        if (codeElement?.Parent?.Parent is CodeNamespace codeNamespace)
        {
            writer.WriteLine($"namespace {codeNamespace.Name.ReplaceDotsWithSlashInNamespaces()};");
            writer.WriteLine();
            codeElement.Usings?
                .Where(x => x.Declaration != null && (x.Declaration.IsExternal ||
                            !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)))
                .Where(static x => string.IsNullOrEmpty(x.Alias))
                .Select(x =>
                {
                    string namespaceValue;
                    if (x.Declaration is { IsExternal: true })
                    {
                        namespaceValue = string.IsNullOrEmpty(x.Declaration.Name) ? string.Empty : $"{x.Declaration.Name.ReplaceDotsWithSlashInNamespaces()}\\";
                        return
                            $"use {namespaceValue}{x.Name.ReplaceDotsWithSlashInNamespaces()};";
                    }
                    namespaceValue = string.IsNullOrEmpty(x.Name) ? string.Empty : $"{x.Name.ReplaceDotsWithSlashInNamespaces()}\\";
                    return
                        $"use {namespaceValue}{x.Declaration!.Name.ReplaceDotsWithSlashInNamespaces()};";
                })
                    .Distinct()
                .OrderBy(x => x)
                .ToList()
                .ForEach(x =>
                {
                    hasUse = true;
                    writer.WriteLine(x);
                });
        }
        if (hasUse)
        {
            writer.WriteLine(string.Empty);
        }
    }
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        var codeParameters = pathParameters?.ToArray();
        var codePathParametersSuffix = codeParameters?.Length > 0 ? $", {string.Join(", ", codeParameters.Select(x => $"${x.Name.ToFirstCharacterLowerCase()}"))}" : string.Empty;
        var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) && parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty ?
            $"$this->{pathParametersProperty.Name}" :
            urlTemplateVarName;
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
            writer.WriteLines($"return new {returnType}(${urlTemplateParams}, $this->{requestAdapterProperty.Name}{codePathParametersSuffix});");
    }
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        writer.WriteLine($"${TempDictionaryVarName} = {pathParametersReference};");
        if (parameters.Length != 0)
            writer.WriteLines(parameters.Select(p =>
                $"${TempDictionaryVarName}['{p.Item2}'] = {p.Item3};"
            ));
    }

    private static bool IsSymbolDuplicated(string symbol, CodeElement targetElement)
    {
        var targetClass = targetElement?.GetImmediateParentOfType<CodeClass>();
        if (targetClass?.Parent is CodeClass parentClass)
            targetClass = parentClass;
        return targetClass?.StartBlock
            ?.Usings
            ?.Where(x => !x.IsExternal && symbol.Equals(x.Declaration?.TypeDefinition?.Name, StringComparison.OrdinalIgnoreCase))
            ?.Distinct(_usingDeclarationNameComparer)
            ?.Count() > 1;
    }
}
