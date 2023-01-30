using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;
public class RubyConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "stdin";
    private const string InternalVoidTypeName = "nil";
    public override string VoidTypeName => InternalVoidTypeName;
    public override string DocCommentPrefix => "## ";
    public override string ParseNodeInterfaceName => "parse_node";
    internal string DocCommentStart = "## ";
    internal string DocCommentEnd = "## ";
    public override string TempDictionaryVarName => "url_tpl_params";
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private",
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        var defaultValue = parameter.Optional && (targetElement is not CodeMethod currentMethod || !currentMethod.IsOfKind(CodeMethodKind.Setter)) ? 
            $"={parameter.DefaultValue ?? "nil"}" :
            string.Empty;
        return $"{parameter.Name}{defaultValue}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeType currentType) {
            return $"{TranslateType(currentType)}";
        }

        throw new InvalidOperationException();
    }
    public override string TranslateType(CodeType type)
    {
        return type.Name switch {
            "integer" => "number",
            "float" or "string" or "object" or "boolean" or "void" => type.Name, // little casing hack
            _ => type.Name.ToFirstCharacterUpperCase() ?? "object",
        };
    }
    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        if(!string.IsNullOrEmpty(description)) {
            writer.WriteLine($"{DocCommentPrefix}");
            writer.WriteLine($"# {description}");
        }
    }
    #pragma warning disable CA1822 // Method should be static
    public string GetNormalizedNamespacePrefixForType(CodeTypeBase type)
    {
        if(type is CodeType xType)
            if ((xType.TypeDefinition is CodeClass || xType.TypeDefinition is CodeEnum) &&
                xType.TypeDefinition.Parent is CodeNamespace ns)
                return $"{ns.Name.NormalizeNameSpaceName("::")}::";
            else if (xType.TypeDefinition is CodeType definition && definition.IsExternal && !string.IsNullOrEmpty(definition.Name))
                return $"{definition.Name}::";
        return string.Empty;
    }
    #pragma warning restore CA1822 // Method should be static
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription.Replace("\\", "#");
    #pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, string? prefix = default, IEnumerable<CodeParameter>? pathParameters = default) {
        if(parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp) {
            var urlTemplateParams = urlTemplateVarName ?? $"@{pathParametersProp.Name.ToSnakeCase()}";
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(static x => $"{x.Name}"))}";
            writer.WriteLine($"{prefix}{returnType.ToFirstCharacterUpperCase()}.new({urlTemplateParams}, @{requestAdapterProp.Name.ToSnakeCase()}{pathParametersSuffix})");
        }
    }
    #pragma warning restore CA1822 // Method should be static
}
