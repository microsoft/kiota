using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CSharpConventionService : ILanguageConventionService {
        public string StreamTypeName => "stream";
        public string VoidTypeName => "void";
        public string DocCommentPrefix => "/// ";
        public string PathSegmentPropertyName => "PathSegment";
        public string CurrentPathPropertyName => "CurrentPath";
        public string HttpCorePropertyName => "HttpCore";
        public HashSet<string> NullableTypes { get; } = new() { "int", "bool", "float", "double", "decimal", "Guid", "DateTimeOffset" };
        public static string NullableMarker => "?";
        public string ParseNodeInterfaceName => "IParseNode";
        public object RawUrlPropertyName => "IsRawUrl";
        public void WriteShortDescription(string description, LanguageWriter writer) {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}<summary>{description}</summary>");
        }
        public string GetAccessModifier(AccessModifier access)
        {
            return (access) switch {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private",
            };
        }
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default, string prefix = default) {
            var currentPath = addCurrentPath ? $"{CurrentPathPropertyName} + " : string.Empty;
            writer.WriteLine($"{prefix}new {returnType}({currentPath}{PathSegmentPropertyName} {suffix}, {HttpCorePropertyName}, false);");
        }
        internal bool ShouldTypeHaveNullableMarker(CodeTypeBase propType, string propTypeName) {
            return propType.IsNullable && (NullableTypes.Contains(propTypeName) || (propType is CodeType codeType && codeType.TypeDefinition is CodeEnum));
        }
        public string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var nullableSuffix = ShouldTypeHaveNullableMarker(code, typeName) ? NullableMarker : string.Empty;
                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.Complex => ">",
                    CodeType.CodeTypeCollectionKind.Array => "[]",
                    _ => string.Empty,
                };
                if (currentType.ActionOf)
                    return $"Action<{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public string TranslateType(string typeName)
        {
            switch (typeName)
            {
                case "integer": return "int";
                case "boolean": return "bool";
                case "string": return "string"; // little casing hack
                case "object": return "object";
                case "void": return "void";
                default: return typeName?.ToFirstCharacterUpperCase() ?? "object";
            }
        }
        public bool IsPrimitiveType(string typeName) {
            return !string.IsNullOrEmpty(typeName) &&
                        (NullableTypes.Contains(typeName) ||
                        "string".Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }
        public string GetParameterSignature(CodeParameter parameter)
        {
            var parameterType = GetTypeString(parameter.Type);
            var defaultValue = (parameter) switch {
                _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
                _ when parameter.Optional => " = default",
                _ => string.Empty,
            };
            return $"{parameterType} {parameter.Name}{defaultValue}";
        }
    }
}
