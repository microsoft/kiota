using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeTypeBase;

namespace Kiota.Builder.Writers.Python;
public class PythonConventionService : CommonLanguageConventionService
{
    public PythonConventionService(LanguageWriter languageWriter)
    {
        writer = languageWriter;
    }
    private readonly LanguageWriter writer;
    public override string StreamTypeName => "bytes";
    public override string VoidTypeName => "None";
    public override string DocCommentPrefix => "";
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "\"\"\"";
    internal string DocCommentEnd = "\"\"\"";
    internal string InLineCommentPrefix = "# ";
    public override string TempDictionaryVarName => "url_tpl_params";
    
    #pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, IEnumerable<CodeParameter> pathParameters = default) {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var urlTemplateParams = urlTemplateVarName ?? $"self.{pathParametersProperty.Name.ToSnakeCase()}";
        var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name}"))}";
        writer.WriteLine($"return {returnType}({urlTemplateParams}, self.{requestAdapterProp.Name.ToSnakeCase()}{pathParametersSuffix})");
    }
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters) {
        if(pathParametersType == null) return;
        writer.WriteLine($"{TempDictionaryVarName} = get_path_parameters({pathParametersReference.ToSnakeCase()})");
        if(parameters.Any())
            writer.WriteLines(parameters.Select(p => 
                $"{TempDictionaryVarName}[\"{p.Item2}\"] = {p.Item3}"
            ).ToArray());
    }

    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch {
            AccessModifier.Public => "",
            AccessModifier.Protected => "_",
            _ => "",
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
    {
        var defaultValueSuffiix = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" = {parameter.DefaultValue}";
        return $"{parameter.Name.ToSnakeCase()}: {(parameter.Type.IsNullable ? "Optional[" : string.Empty)}{GetTypeString(parameter.Type, targetElement)}{(parameter.Type.IsNullable ? "]": string.Empty)}{defaultValueSuffiix}";
    }
    private static string GetTypeAlias(CodeType targetType, CodeElement targetElement) {
        var parentBlock = targetElement.GetImmediateParentOfType<IBlock>();
        if(parentBlock != null) {
            var aliasedUsing = parentBlock.Usings
                                                .FirstOrDefault(x => !x.IsExternal &&
                                                                x.Declaration.TypeDefinition == targetType.TypeDefinition &&
                                                                !string.IsNullOrEmpty(x.Alias));
            return aliasedUsing?.Alias;
        }
        return null;
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true) {
        if(code is null)
            return null;
        var collectionPrefix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "List[";
        var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "]";
        if(code is CodeUnionType currentUnion && currentUnion.Types.Any())
            return currentUnion.Types.Select(x => GetTypeString(x, targetElement)).Aggregate((x, y) => $"Union[{x}, {y.ToFirstCharacterLowerCase()}]");
        else if(code is CodeType currentType) {
            var typeName = GetTypeAlias(currentType, targetElement) ?? TranslateType(currentType);
            if (code.ActionOf)
                return WriteInlineDeclaration(currentType, targetElement);
            else
                return $"{collectionPrefix}{typeName}{collectionSuffix}";
        }
        else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }
    #pragma warning restore CA1822 // Method should be static
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
    public override string TranslateType(CodeType type)

    {
        return type.Name switch  {
            "String" or "string" => "str",
            "integer" or "int32" or "int64" => "int",
            "decimal" or "double" => "float",
            "bytes" => "bytes",
            "void" => "None",
            "datetime" => "datetime",
            "DateTimeOffset" => "timedelta",
            "boolean" => "bool",
            "Object" or "object" or "float" => type.Name.ToSnakeCase(),
            _ => type.Name.ToFirstCharacterUpperCase().Replace("IParseNode", "ParseNode") ?? "object",
        };
    }
    #pragma warning disable CA1822 // Method should be static
    public bool IsPrimitiveType(string typeName) {
        return typeName switch {
            "int" or "float" or "str" or "bool" or "None" => true,
            _ => false,
        };
    }

    private string WriteInlineDeclaration(CodeType currentType, CodeElement targetElement) {
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
        if(string.IsNullOrEmpty(innerDeclaration))
            return "object";
        else
            return $"{{{innerDeclaration}}}";
    }
    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        var isDescriptionPresent = !string.IsNullOrEmpty(description);
        if(isDescriptionPresent) {
            writer.WriteLine(DocCommentStart);
            writer.WriteLine($"{RemoveInvalidDescriptionCharacters(description)}");
            writer.WriteLine(DocCommentEnd);
        }
    }

    public void WriteInLineDescription(string description, LanguageWriter writer)
    {
        var isDescriptionPresent = !string.IsNullOrEmpty(description);
        if(isDescriptionPresent) {
            writer.WriteLine($"{InLineCommentPrefix}{RemoveInvalidDescriptionCharacters(description)}");
        }
    }
}
