using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeTypeBase;

namespace Kiota.Builder.Writers.Swift {
    public class SwiftConventionService : CommonLanguageConventionService
    {
        // TODO (Swift) reevaluate entire file for Swift
        public override string StreamTypeName => "stream";
        public override string VoidTypeName => "void";
        public override string DocCommentPrefix => "/// ";
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
                AccessModifier.Protected => "internal",
                _ => "private",
            };
        }
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, string prefix = default, IEnumerable<CodeParameter> pathParameters = default) {
            var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            var urlTplRef = urlTemplateVarName ?? pathParametersProp.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{prefix}new {returnType}({urlTplRef}, {requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix});");
        }
        public override string TempDictionaryVarName => "urlTplParams";
        #pragma warning restore CA1822 // Method should be static
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
                throw new InvalidOperationException($"Swift does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateTypeAndAvoidUsingNamespaceSegmentNames(currentType, targetElement);
                var nullableSuffix = code.IsNullable ? NullableMarkerAsString : string.Empty;
                var collectionPrefix = currentType.IsCollection && includeCollectionInformation ? "[" : string.Empty;
                var collectionSuffix = currentType.IsCollection && includeCollectionInformation ? $"]{nullableSuffix}" : string.Empty;
                if(currentType.IsCollection && !string.IsNullOrEmpty(nullableSuffix))
                    nullableSuffix = string.Empty;

                if (currentType.ActionOf)
                    return $"({collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>) -> Void";
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
                "integer" => "Int32",
                "boolean" => "Bool",
                "float" => "Float32",
                "long" => "Int64",
                "double" or "decimal" => "Float64",
                "guid" => "UUID",
                "void" or "uint8" or "int8" or "int32" or "int64" or "float32" or "float64" or "string" => type.Name.ToFirstCharacterUpperCase(),
                "binary" => "[UInt8]",
                "DateTimeOffset" => "Date", // TODO
                _ => type.Name?.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            var parameterType = GetTypeString(parameter.Type, targetElement);
            var defaultValue = parameter switch {
                _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
                _ when parameter.Optional => " = default", // TODO (Swift) reevaluate
                _ => string.Empty,
            };
            return $"{parameter.Name.ToFirstCharacterLowerCase()} : {parameterType}{defaultValue}";
        }
    }
}
