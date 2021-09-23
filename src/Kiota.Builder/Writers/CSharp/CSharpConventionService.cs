using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeTypeBase;

namespace Kiota.Builder.Writers.CSharp {
    public class CSharpConventionService : CommonLanguageConventionService {
        public override string StreamTypeName => "stream";
        public override string VoidTypeName => "void";
        public override string DocCommentPrefix => "/// ";
        public override string PathSegmentPropertyName => "PathSegment";
        public override string CurrentPathPropertyName => "CurrentPath";
        public override string HttpCorePropertyName => "HttpCore";
        public HashSet<string> NullableTypes { get; } = new(StringComparer.OrdinalIgnoreCase) { "int", "bool", "float", "double", "decimal", "long", "Guid", "DateTimeOffset" };
        public static readonly char NullableMarker = '?';
        public static string NullableMarkerAsString => "?";
        public override string ParseNodeInterfaceName => "IParseNode";
        public override string RawUrlPropertyName => "IsRawUrl";
        public override void WriteShortDescription(string description, LanguageWriter writer) {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}<summary>{description}</summary>");
        }
        public override string GetAccessModifier(AccessModifier access)
        {
            return access switch {
                AccessModifier.Public => "public",
                AccessModifier.Protected => "protected",
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
        public override string GetTypeString(CodeTypeBase code) => throw new InvalidOperationException("Use the overload with the target element parameter instead");

        private static HashSet<string> _reservedNames;
        private static readonly object _reservedNamesLock = new();
        private static HashSet<string> GetReservedNames(CodeElement currentElement) {
            if(_reservedNames == null) {
                lock(_reservedNamesLock) {
                    var rootNamespace = currentElement.GetImmediateParentOfType<CodeNamespace>().GetRootNamespace();
                    var names = new List<string>(GetNamespaceNameSegments(rootNamespace).Distinct(StringComparer.OrdinalIgnoreCase));
                    _reservedNames = new (names, StringComparer.OrdinalIgnoreCase);
                    _reservedNames.Add("keyvaluepair"); //workaround as System.Collections.Generic imports keyvalue pair
                }
            }
            return _reservedNames;
        }
        private static IEnumerable<string> GetNamespaceNameSegments(CodeNamespace ns) {
            if(!string.IsNullOrEmpty(ns.Name))
                foreach(var segment in ns.Name.Split('.', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase))
                    yield return segment;
            foreach(var childNs in ns.Namespaces)
                foreach(var segment in GetNamespaceNameSegments(childNs))
                    yield return segment;
        }
        public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if(code is CodeUnionType)
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType);
                if(currentType.TypeDefinition != null &&
                    GetReservedNames(targetElement).Contains(typeName))
                    typeName = $"{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name}.{typeName}";
                var nullableSuffix = ShouldTypeHaveNullableMarker(code, typeName) ? NullableMarkerAsString : string.Empty;
                var collectionPrefix = currentType.CollectionKind == CodeTypeCollectionKind.Complex && includeCollectionInformation ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch {
                    CodeTypeCollectionKind.Complex when includeCollectionInformation => ">",
                    CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
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
                "int64" => "long",
                "string" or "float" or "double" or "object" or "void" => type.Name.ToLowerInvariant(),// little casing hack
                "binary" => "byte[]",
                _ => type.Name?.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        public bool IsPrimitiveType(string typeName) {
            if (string.IsNullOrEmpty(typeName)) return false;
            typeName = typeName.StripArraySuffix().TrimEnd('?').ToLowerInvariant();
            return typeName switch {
                "string" => true,
                _ when NullableTypes.Contains(typeName) => true,
                _ => false,
            };
        }
        public override string GetParameterSignature(CodeParameter parameter) => throw new InvalidOperationException("Use the overload with the target element parameter instead");
        public string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            var parameterType = GetTypeString(parameter.Type, targetElement);
            var defaultValue = parameter switch {
                _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
                _ when parameter.Optional => " = default",
                _ => string.Empty,
            };
            return $"{parameterType} {parameter.Name}{defaultValue}";
        }
    }
}
