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
            ).ToArray());
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
        if (code is null)
            return string.Empty;
        var collectionPrefix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "List[";
        var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "]";
        if (code is CodeComposedTypeBase currentUnion && currentUnion.Types.Any())
            return currentUnion.Types.Select(x => GetTypeString(x, targetElement, true, writer)).Aggregate((x, y) => $"Union[{x}, {y.ToFirstCharacterLowerCase()}]");
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
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription.Replace("\\", "/");
    public override string TranslateType(CodeType type)
    {
        if (type.IsExternal)
            return TranslateExternalType(type);
        return TranslateInternalType(type);
    }
    private static string TranslateExternalType(CodeType type)
    {
        return type.Name switch
        {
            "String" or "string" => "str",
            "integer" or "int32" or "int64" or "byte" or "sbyte" => "int",
            "decimal" or "double" => "float",
            "Binary" or "binary" or "base64" or "base64url" => "bytes",
            "void" => "None",
            "DateTimeOffset" => "datetime",
            "boolean" => "bool",
            "Object" or "object" or "float" or "bytes" or "datetime" or "timedelta" or "date" or "time" => type.Name,
            _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }
    private static string TranslateInternalType(CodeType type)
    {
        if (type.Name.Contains("RequestConfiguration") && type.TypeDefinition is not null)
            return type.TypeDefinition.Name.ToFirstCharacterUpperCase();
        if (type.Name.Contains("QueryParameters"))
            return type.Name;
        if (type.Name.Contains("APIError"))
            return type.Name;
        return type.Name switch
        {
            "String" or "string" => "str",
            "integer" or "int32" or "int64" or "byte" or "sbyte" => "int",
            "decimal" or "double" => "float",
            "Binary" or "binary" or "base64" or "base64url" => "bytes",
            "void" => "None",
            "DateTimeOffset" => "datetime",
            "boolean" => "bool",
            "Object" or "object" or "float" or "bytes" or "datetime" or "timedelta" or "date" or "time" => type.Name,
            _ => $"{type.Name.ToSnakeCase()}.{type.Name.ToFirstCharacterUpperCase()}" ?? "object",
        };
    }

#pragma warning disable CA1822 // Method should be static
    public bool TypeExistInSameClassAsTarget(CodeTypeBase currentType, CodeElement targetElement)
    {
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
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteLine(DocCommentStart);
            writer.WriteLine($"{RemoveInvalidDescriptionCharacters(description)}");
            writer.WriteLine(DocCommentEnd);
        }
    }

    public void WriteInLineDescription(string description, LanguageWriter writer)
    {
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteLine($"{InLineCommentPrefix}{RemoveInvalidDescriptionCharacters(description)}");
        }
    }
}
