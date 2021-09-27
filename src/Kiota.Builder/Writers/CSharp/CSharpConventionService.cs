using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CSharpConventionService : CommonLanguageConventionService {
        public override string StreamTypeName => "stream";
        public override string VoidTypeName => "void";
        public override string DocCommentPrefix => "/// ";
        public override string PathSegmentPropertyName => "PathSegment";
        public override string CurrentPathPropertyName => "CurrentPath";
        public override string HttpCorePropertyName => "HttpCore";
        public HashSet<string> NullableTypes { get; } = new() { "int", "bool", "float", "double", "decimal", "Guid", "DateTimeOffset" };
        public static string NullableMarker => "?";
        public override string ParseNodeInterfaceName => "IParseNode";
        public override string RawUrlPropertyName => "IsRawUrl";
        public override void WriteShortDescription(string description, LanguageWriter writer) {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}<summary>{description}</summary>");
        }
        public override string GetAccessModifier(AccessModifier access)
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
        public override string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType);
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

        public override string TranslateType(CodeType type)
        {
            return type.Name switch
            {
                "integer" => "int",
                "boolean" => "bool",
                "string" => "string",// little casing hack
                "object" => "object",
                "void" => "void",
                "binary" => "byte[]",
                _ => type.Name?.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        public bool IsPrimitiveType(string typeName) {
            return !string.IsNullOrEmpty(typeName) &&
                        (NullableTypes.Contains(typeName) ||
                        "string".Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }
        public override string GetParameterSignature(CodeParameter parameter)
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
