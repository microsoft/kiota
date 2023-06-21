using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.Python;
public class PythonConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "bytes";
    public override string VoidTypeName => "None";
    public override string DocCommentPrefix => "";
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "\"\"\"";
    internal string DocCommentEnd = "\"\"\"";
    internal string InLineCommentPrefix = "# ";
    public override string TempDictionaryVarName => "url_tpl_params";

#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) && parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty ?
            $"self.{pathParametersProperty.Name.ToSnakeCase()}" :
            urlTemplateVarName;
        var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToSnakeCase()}"))}";
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
            writer.WriteLine($"return {returnType}(self.{requestAdapterProp.Name.ToSnakeCase()}, {urlTemplateParams}{pathParametersSuffix})");
    }
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase? pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        writer.WriteLine($"{TempDictionaryVarName} = get_path_parameters({pathParametersReference.ToSnakeCase()})");
        if (parameters.Any())
            writer.WriteLines(parameters.Select(p =>
                $"{TempDictionaryVarName}[\"{p.Item2}\"] = {p.Item3}"
            ));
    }

    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "",
            AccessModifier.Protected => "_",
            _ => "",
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(targetElement);
        var defaultValueSuffix = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" = {parameter.DefaultValue}";
        return $"{parameter.Name.ToSnakeCase()}: {(parameter.Type.IsNullable ? "Optional[" : string.Empty)}{GetTypeString(parameter.Type, targetElement, true, writer)}{(parameter.Type.IsNullable ? "] = None" : string.Empty)}{defaultValueSuffix}";
    }
    private static string GetTypeAlias(CodeType targetType, CodeElement targetElement)
    {
        if (targetElement.GetImmediateParentOfType<IBlock>() is IBlock parentBlock)
        {
            var aliasedUsing = parentBlock.Usings
                                                .FirstOrDefault(x => !x.IsExternal &&
                                                                x.Declaration?.TypeDefinition == targetType.TypeDefinition &&
                                                                !string.IsNullOrEmpty(x.Alias));
            return aliasedUsing?.Alias ?? string.Empty;
        }
        return string.Empty;
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(targetElement);
        if (code is null)
            return string.Empty;
        var collectionPrefix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "List[";
        var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "]";
        if (code is CodeComposedTypeBase currentUnion && currentUnion.Types.Any())
            return currentUnion.Types.Select(x => GetTypeString(x, targetElement, true, writer)).Aggregate((x, y) => $"Union[{x}, {TranslateAllTypes(y)}]");
        if (code is CodeType currentType)
        {
            var alias = GetTypeAlias(currentType, targetElement);
            var typeName = string.IsNullOrEmpty(alias) ? TranslateType(currentType) : alias;
            if (TypeExistInSameClassAsTarget(code, targetElement) && targetElement.Parent != null)
                typeName = targetElement.Parent.Name.ToFirstCharacterUpperCase();
            if (code.ActionOf && writer != null)
                return WriteInlineDeclaration(currentType, targetElement, writer);
            return $"{collectionPrefix}{typeName}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }
#pragma warning restore CA1822 // Method should be static
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription.Replace("\\", "/", StringComparison.OrdinalIgnoreCase);
    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return TranslateAllTypes(type.Name);
    }
    private static string TranslateAllTypes(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "string" => "str",
            "integer" or "int32" or "int64" or "long" or "byte" or "sbyte" => "int",
            "decimal" or "double" => "float",
            "binary" or "base64" or "base64url" => "bytes",
            "void" => "None",
            "datetimeoffset" => "datetime.datetime",
            "boolean" => "bool",
            "guid" or "uuid" => "UUID",
            "object" or "str" or "int" or "float" or "bytes" or "datetime.datetime" or "datetime.timedelta" or "datetime.date" or "datetime.time" => typeName.ToLowerInvariant(),
            _ => !string.IsNullOrEmpty(typeName) ? typeName.ToFirstCharacterUpperCase() : "object",
        };
    }

#pragma warning disable CA1822 // Method should be static
    public bool TypeExistInSameClassAsTarget(CodeTypeBase currentType, CodeElement targetElement)
    {
        ArgumentNullException.ThrowIfNull(currentType);
        ArgumentNullException.ThrowIfNull(targetElement);
        return targetElement.Parent is CodeClass && currentType.Name == targetElement.Parent.Name;
    }
#pragma warning disable CA1822 // Method should be static
    public bool IsPrimitiveType(string typeName)
    {
        return typeName switch
        {
            "int" or "float" or "str" or "bool" or "None" => true,
            _ => false,
        };
    }

    private string WriteInlineDeclaration(CodeType currentType, CodeElement targetElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.IncreaseIndent(4);
        var childElements = (currentType?.TypeDefinition as CodeClass)
                                    ?.Properties
                                    ?.OrderBy(x => x.Name)
                                    ?.Select(x => $"{x.Name}?: {GetTypeString(x.Type, targetElement, true, writer)}");
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
    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteLine(DocCommentStart);
            writer.WriteLine($"{RemoveInvalidDescriptionCharacters(description)}");
            writer.WriteLine(DocCommentEnd);
        }
    }

    public void WriteInLineDescription(string description, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteLine($"{InLineCommentPrefix}{RemoveInvalidDescriptionCharacters(description)}");
        }
    }
}
