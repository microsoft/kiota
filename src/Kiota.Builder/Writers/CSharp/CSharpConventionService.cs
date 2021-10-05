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
        private static readonly HashSet<string> NullableTypes = new(StringComparer.OrdinalIgnoreCase) { "int", "bool", "float", "double", "decimal", "long", "Guid", "DateTimeOffset" };
        public static readonly char NullableMarker = '?';
        public static string NullableMarkerAsString => "?";
        public override string ParseNodeInterfaceName => "IParseNode";
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
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, string prefix = default, IEnumerable<CodeParameter> pathParameters = default) {
            var urlTemplateParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplateParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name}"))}";
            var urlTplRef = urlTemplateVarName ?? urlTemplateParametersProp.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{prefix}new {returnType}({urlTplRef}, {requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix});");
        }
        #pragma warning restore CA1822 // Method should be static
        internal bool ShouldTypeHaveNullableMarker(CodeTypeBase propType, string propTypeName) {
            return propType.IsNullable && (NullableTypes.Contains(propTypeName) || (propType is CodeType codeType && codeType.TypeDefinition is CodeEnum));
        }
        private static HashSet<string> _namespaceSegmentsNames;
        private static readonly object _namespaceSegmentsNamesLock = new();
        private static HashSet<string> GetNamesInUseByNamespaceSegments(CodeElement currentElement) {
            if(_namespaceSegmentsNames == null) {
                lock(_namespaceSegmentsNamesLock) {
                    var rootNamespace = currentElement.GetImmediateParentOfType<CodeNamespace>().GetRootNamespace();
                    var names = new List<string>(GetNamespaceNameSegments(rootNamespace).Distinct(StringComparer.OrdinalIgnoreCase));
                    _namespaceSegmentsNames = new (names, StringComparer.OrdinalIgnoreCase);
                    _namespaceSegmentsNames.Add("keyvaluepair"); //workaround as System.Collections.Generic imports keyvalue pair
                }
            }
            return _namespaceSegmentsNames;
        }
        private static IEnumerable<string> GetNamespaceNameSegments(CodeNamespace ns) {
            if(!string.IsNullOrEmpty(ns.Name))
                foreach(var segment in ns.Name.Split('.', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase))
                    yield return segment;
            foreach(var childNs in ns.Namespaces)
                foreach(var segment in GetNamespaceNameSegments(childNs))
                    yield return segment;
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if(code is CodeUnionType)
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateTypeAndAvoidUsingNamespaceSegmentNames(currentType, targetElement);
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
        private string TranslateTypeAndAvoidUsingNamespaceSegmentNames(CodeType currentType, CodeElement targetElement)
        {
            var typeName = TranslateType(currentType);
            if(currentType.TypeDefinition != null &&
                GetNamesInUseByNamespaceSegments(targetElement).Contains(typeName))
                return $"{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name}.{typeName}";
            else
                return typeName;
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
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
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
