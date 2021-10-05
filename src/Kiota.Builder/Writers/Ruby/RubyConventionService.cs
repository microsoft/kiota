using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class RubyConventionService : CommonLanguageConventionService
    {
        public static string SerializerFactoryPropertyName => "serializer_factory";
        public override string StreamTypeName => "stdin";
        private const string InternalVoidTypeName = "nil";
        public override string VoidTypeName => InternalVoidTypeName;
        public override string DocCommentPrefix => "## ";
        private const string PathSegmentPropertyName = "path_segment";
        private const string CurrentPathPropertyName = "current_path";
        private const string HttpCorePropertyName = "http_core";
        public override string ParseNodeInterfaceName => "parse_node";
        internal string DocCommentStart = "## ";
        internal string DocCommentEnd = "## ";
        public override string GetAccessModifier(AccessModifier access)
        {
            return access switch {
                AccessModifier.Public => "public",
                AccessModifier.Protected => "protected",
                _ => "private",
            };
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            var defaultValue = parameter.Optional ? $"={(parameter.DefaultValue ?? "nil")}" : string.Empty;
            return $"{parameter.Name}{defaultValue}";
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if (code is CodeType currentType) {
                return $"{TranslateType(currentType)}";
            }
            else throw new InvalidOperationException();
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
        public string GetNormalizedNamespacePrefixForType(CodeTypeBase type) {
            if(type is CodeType xType && 
                (xType.TypeDefinition is CodeClass || xType.TypeDefinition is CodeEnum) &&
                xType.TypeDefinition.Parent is CodeNamespace ns)
                return $"{ns.Name.NormalizeNameSpaceName("::")}::";
            else return string.Empty;
        }
        #pragma warning restore CA1822 // Method should be static
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "#");
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default, string prefix = default, IEnumerable<CodeParameter> pathParameters = default) {
            var currentPath = addCurrentPath ? $"@{CurrentPathPropertyName} + " : string.Empty;
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $"{string.Join(", ", pathParameters.Select(x => $"{x.Name}"))}, ";
            writer.WriteLine($"{prefix}{returnType.ToFirstCharacterUpperCase()}.new({currentPath}@{PathSegmentPropertyName} {suffix}, @{HttpCorePropertyName}, {pathParametersSuffix}false)");
        }
        #pragma warning restore CA1822 // Method should be static
    }
}
