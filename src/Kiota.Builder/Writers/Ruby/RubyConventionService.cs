using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class RubyConventionService : ILanguageConventionService
    {
        public string SerializerFactoryPropertyName => "serializer_factory";
        public string StreamTypeName => "stdin";
        private const string _voidTypeName = "nil";
        public string VoidTypeName => _voidTypeName;
        public string DocCommentPrefix => "## ";
        public string PathSegmentPropertyName => "path_segment";
        public string CurrentPathPropertyName => "current_path";
        public string HttpCorePropertyName => "http_core";
        public string ParseNodeInterfaceName => "parse_node";
        internal string DocCommentStart = "## ";
        internal string DocCommentEnd = "## ";
        public string GetAccessModifier(AccessModifier access)
        {
            return (access) switch {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private",
            };
        }
        public string GetParameterSignature(CodeParameter parameter)
        {
            return $"{parameter.Name}";
        }
        public string GetTypeString(CodeTypeBase code)
        {
            if (code is CodeType currentType) {
                return $"{TranslateType(currentType.Name)}";
            }
            else throw new InvalidOperationException();
        }
        public string TranslateType(string typeName)
        {
            return (typeName) switch {
                ("void") => typeName.ToFirstCharacterLowerCase(),
                _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}");
                writer.WriteLine($"# {description}");
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "#");
        internal void AddRequestBuilderBody(string returnType, LanguageWriter writer, string suffix = default) {
            writer.WriteLines($"parent_path = (@{CurrentPathPropertyName.ToSnakeCase()} == null ? \"\" : @{CurrentPathPropertyName.ToSnakeCase()}) + @{PathSegmentPropertyName.ToSnakeCase()}",
                        $"parent_core = @{HttpCorePropertyName.ToSnakeCase()}",
                        $"{returnType.ToSnakeCase()} = {returnType}.new",
                        $"{returnType.ToSnakeCase()}.{CurrentPathPropertyName.ToSnakeCase()} = parent_path",
                        $"{returnType.ToSnakeCase()}.{HttpCorePropertyName.ToSnakeCase()} = parent_core",
                        $"return {returnType.ToSnakeCase()}");
        }
    }
}
