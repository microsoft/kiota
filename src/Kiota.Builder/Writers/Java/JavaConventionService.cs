using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Java;
public class JavaConventionService : CommonLanguageConventionService
{
    private const string InternalStreamTypeName = "InputStream";
    public override string StreamTypeName => InternalStreamTypeName;
    private const string InternalVoidTypeName = "Void";
    public override string VoidTypeName => InternalVoidTypeName;
    public override string DocCommentPrefix => " * ";
    internal HashSet<string> PrimitiveTypes = new() { "String", "Boolean", "Integer", "Float", "Long", "Guid", "UUID", "Double", "OffsetDateTime", "LocalDate", "LocalTime", "Period", "Byte", "Short", "BigDecimal", InternalVoidTypeName, InternalStreamTypeName };
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "/**";
    internal string DocCommentEnd = " */";
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private",
        };
    }

    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        var nullKeyword = parameter.Optional ? "Nullable" : "Nonnull";
        var nullAnnotation = parameter.Type.IsNullable ? $"@javax.annotation.{nullKeyword} " : string.Empty;
        return $"{nullAnnotation}final {GetTypeString(parameter.Type, targetElement)} {parameter.Name.ToFirstCharacterLowerCase()}";
    }

    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var typeName = TranslateType(currentType);
            if (!currentType.IsExternal && IsSymbolDuplicated(typeName, targetElement) && currentType.TypeDefinition is not null)
                typeName = $"{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name}.{typeName}";

            var collectionPrefix = currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.Complex && includeCollectionInformation ? "java.util.List<" : string.Empty;
            var collectionSuffix = currentType.CollectionKind switch
            {
                CodeTypeBase.CodeTypeCollectionKind.Complex when includeCollectionInformation => ">",
                CodeTypeBase.CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
                _ => string.Empty,
            };
            if (currentType.ActionOf)
                return $"java.util.function.Consumer<{collectionPrefix}{typeName}{collectionSuffix}>";
            return $"{collectionPrefix}{typeName}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }
    private static readonly CodeUsingDeclarationNameComparer usingDeclarationComparer = new();
    private static bool IsSymbolDuplicated(string symbol, CodeElement targetElement)
    {
        var targetClass = targetElement as CodeClass ?? targetElement.GetImmediateParentOfType<CodeClass>();
        if (targetClass.Parent is CodeClass parentClass)
            targetClass = parentClass;
        return targetClass.StartBlock
                        ?.Usings
                        ?.Where(x => !x.IsExternal && symbol.Equals(x.Declaration?.TypeDefinition?.Name, StringComparison.OrdinalIgnoreCase))
                        ?.Distinct(usingDeclarationComparer)
                        ?.Count() > 1;
    }
    public override string TranslateType(CodeType type)
    {
        return type.Name switch
        {
            "int64" => "Long",
            "sbyte" => "Short",
            "decimal" => "BigDecimal",
            "void" or "boolean" when !type.IsNullable => type.Name, //little casing hack
            "binary" or "base64" or "base64url" => "byte[]",
            "Guid" => "UUID",
            _ when type.Name.Contains('.') => type.Name, // casing
            _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "Object",
        };
    }
    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        if (!string.IsNullOrEmpty(description))
            writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)}{DocCommentEnd}");
    }
    public void WriteLongDescription(CodeDocumentation documentation, LanguageWriter writer, IEnumerable<string>? additionalRemarks = default)
    {
        if (documentation is null) return;
        if (additionalRemarks == default)
            additionalRemarks = Enumerable.Empty<string>();
        if (documentation.DescriptionAvailable || documentation.ExternalDocumentationAvailable || additionalRemarks.Any())
        {
            writer.WriteLine(DocCommentStart);
            if (documentation.DescriptionAvailable)
                writer.WriteLine($"{DocCommentPrefix}{RemoveInvalidDescriptionCharacters(documentation.Description)}");
            foreach (var additionalRemark in additionalRemarks.Where(static x => !string.IsNullOrEmpty(x)))
                writer.WriteLine($"{DocCommentPrefix}{additionalRemark}");

            if (documentation.ExternalDocumentationAvailable)
                writer.WriteLine($"{DocCommentPrefix}@see <a href=\"{documentation.DocumentationLink}\">{documentation.DocumentationLabel}</a>");
            writer.WriteLine(DocCommentEnd);
        }
    }
    private static readonly Regex nonAsciiReplaceRegex = new(@"[^\u0000-\u007F]+", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) =>
        string.IsNullOrEmpty(originalDescription) ?
            originalDescription :
            nonAsciiReplaceRegex.Replace(originalDescription.Replace("\\", "/").Replace("*/", string.Empty), string.Empty);
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) ? pathParametersProperty?.Name : urlTemplateVarName;
        var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
        writer.WriteLines($"return new {returnType}({urlTemplateParams}, {requestAdapterProp?.Name}{pathParametersSuffix});");
    }
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        var mapTypeName = pathParametersType.Name;
        writer.WriteLine($"final {mapTypeName} {TempDictionaryVarName} = new {mapTypeName}({pathParametersReference});");
        if (parameters.Any())
            writer.WriteLines(parameters.Select(p =>
                $"{TempDictionaryVarName}.put(\"{p.Item2}\", {p.Item3});"
            ));
    }
#pragma warning restore CA1822 // Method should be static
}
