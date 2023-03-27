using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.TypeScript;
public class TypeScriptConventionService : CommonLanguageConventionService
{
    public TypeScriptConventionService(LanguageWriter languageWriter)
    {
        writer = languageWriter;
    }
    private readonly LanguageWriter writer;
    public override string StreamTypeName => "ArrayBuffer";

    public override string VoidTypeName => throw new NotImplementedException();

    public override string DocCommentPrefix => " * ";
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "/**";
    internal string DocCommentEnd = " */";
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        var codePathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => x.Name.ToFirstCharacterLowerCase()))}";
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
        {
            var urlTemplateParams = !string.IsNullOrEmpty(urlTemplateVarName) ? urlTemplateVarName : $"this.{pathParametersProperty.Name}";
            writer.WriteLines($"return new {returnType}({urlTemplateParams}, this.{requestAdapterProp.Name}{codePathParametersSuffix});");
        }
    }
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, string varName = "", params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        if (string.IsNullOrEmpty(varName))
        {
            varName = TempDictionaryVarName;
            writer.WriteLine($"const {varName} = getPathParameters({pathParametersReference});");
        }
        if (parameters.Any())
            writer.WriteLines(parameters.Select(p =>
                $"{varName}[\"{p.Item2}\"] = {p.Item3}"
            ).ToArray());
    }
#pragma warning restore CA1822 // Method should be static
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
        var defaultValueSuffix = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" = {parameter.DefaultValue}";
        return $"{parameter.Name.ToFirstCharacterLowerCase()}{(parameter.Optional && parameter.Type.IsNullable ? "?" : string.Empty)}: {GetTypeString(parameter.Type, targetElement)}{(parameter.Type.IsNullable ? " | undefined" : string.Empty)}{defaultValueSuffix}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None || !includeCollectionInformation ? string.Empty : "[]";
        if (code is CodeComposedTypeBase currentUnion && currentUnion.Types.Any())
            return string.Join(" | ", currentUnion.Types.Select(x => GetTypeString(x, targetElement))) + collectionSuffix;
        if (code is CodeType currentType)
        {
            var typeName = GetTypeAlias(currentType, targetElement) is string alias && !string.IsNullOrEmpty(alias) ? alias : TranslateType(currentType);
            if (code.ActionOf)
                return WriteInlineDeclaration(currentType, targetElement);
            return $"{typeName}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }
    private static string GetTypeAlias(CodeType targetType, CodeElement targetElement)
    {
        if (targetElement.GetImmediateParentOfType<IBlock>() is IBlock parentBlock &&
            parentBlock.Usings
                        .FirstOrDefault(x => !x.IsExternal &&
                                        x.Declaration?.TypeDefinition != null &&
                                        x.Declaration.TypeDefinition == targetType.TypeDefinition &&
                                        !string.IsNullOrEmpty(x.Alias)) is CodeUsing aliasedUsing)
            return aliasedUsing.Alias;
        return string.Empty;
    }
    private string WriteInlineDeclaration(CodeType currentType, CodeElement targetElement)
    {
        writer.IncreaseIndent(4);
        var childElements = (currentType?.TypeDefinition as CodeClass)
                                    ?.Properties
                                    ?.OrderBy(x => x.Name)
                                    ?.Select(x => $"{x.Name}?: {GetTypeString(x.Type, targetElement)}");
        var innerDeclaration = childElements?.Any() ?? false ?
                                        LanguageWriter.NewLine +
                                        writer.GetIndent() +
                                        childElements
                                        .Aggregate((x, y) => $"{x};{LanguageWriter.NewLine}{writer.GetIndent()}{y}")
                                        .Replace(';', ',') +
                                        LanguageWriter.NewLine +
                                        writer.GetIndent()
                                    : string.Empty;
        writer.DecreaseIndent();
        if (string.IsNullOrEmpty(innerDeclaration))
            return "object";
        return $"{{{innerDeclaration}}}";
    }

    public override string TranslateType(CodeType type)
    {
        return type.Name switch
        {
            "integer" or "int64" or "float" or "double" or "byte" or "sbyte" or "decimal" => "number",
            "binary" or "base64" or "base64url" or "Guid" => "string",
            "String" or "Object" or "Boolean" or "Void" or "string" or "object" or "boolean" or "void" => type.Name.ToFirstCharacterLowerCase(), // little casing hack
            _ => GetCodeTypeName(type) is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }

    private static string GetCodeTypeName(CodeType codeType)
    {
        if (codeType.TypeDefinition is CodeFunction)
        {
            return !string.IsNullOrEmpty(codeType.TypeDefinition?.Name) ? codeType.TypeDefinition.Name : string.Empty;
        }

        return (!string.IsNullOrEmpty(codeType.TypeDefinition?.Name) ? codeType.TypeDefinition.Name : codeType.Name).ToFirstCharacterUpperCase();
    }
#pragma warning disable CA1822 // Method should be static
    public bool IsPrimitiveType(string typeName)
    {
        return typeName switch
        {
            "number" or "string" or "byte[]" or "boolean" or "void" => true,
            _ => false,
        };
    }
#pragma warning restore CA1822 // Method should be static
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/") ?? string.Empty;
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
                writer.WriteLine($"{DocCommentPrefix}@see {{@link {documentation.DocumentationLink}|{documentation.DocumentationLabel}}}");
            writer.WriteLine(DocCommentEnd);
        }
    }

}
