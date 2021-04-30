using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CSharpConventionService {
        public string SerializerFactoryPropertyName {get; private set; } = "SerializerFactory";
        public string StreamTypeName {get; private set; } = "stream";
        public string VoidTypeName {get; private set; } = "void";
        public string DocCommentPrefix {get; private set; } = "/// ";
        public string PathSegmentPropertyName {get; private set; } = "PathSegment";
        public string CurrentPathPropertyName {get; private set; } = "CurrentPath";
        public string HttpCorePropertyName {get; private set; } = "HttpCore";
        public string[] NullableTypes {get; private set; } = { "int", "bool", "float", "double", "decimal", "Guid", "DateTimeOffset" };
        public string NullableMarker {get; private set;} = "?";
        public string ParseNodeInterfaceName {get; private set; } = "IParseNode";
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
        internal void AddRequestBuilderBody(string returnType, LanguageWriter writer, string suffix = default, string prefix = default) {
            writer.WriteLine($"{prefix}new {returnType} {{ {HttpCorePropertyName} = {HttpCorePropertyName}, {SerializerFactoryPropertyName} = {SerializerFactoryPropertyName}, {CurrentPathPropertyName} = {CurrentPathPropertyName} + {PathSegmentPropertyName} {suffix}}};");
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
                var collectionSuffix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? ">" : 
                                            (currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Array ? "[]" : string.Empty);
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
        public string GetParameterSignature(CodeParameter parameter)
        {
            var parameterType = GetTypeString(parameter.Type);
            return $"{parameterType} {parameter.Name}{(parameter.Optional ? $" = default": string.Empty)}";
        }
    }
}
