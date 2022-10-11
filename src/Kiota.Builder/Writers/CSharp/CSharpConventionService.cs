using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.CSharp {
    public class CSharpConventionService : CommonLanguageConventionService {
        public override string StreamTypeName => "stream";
        public override string VoidTypeName => "void";
        public override string DocCommentPrefix => "/// ";
        private static readonly HashSet<string> NullableTypes = new(StringComparer.OrdinalIgnoreCase) { "int", "bool", "float", "double", "decimal", "long", "Guid", "DateTimeOffset", "TimeSpan", "Date","Time", "sbyte", "byte" };
        public static readonly char NullableMarker = '?';
        public static string NullableMarkerAsString => "?";
        public override string ParseNodeInterfaceName => "IParseNode";
        public override void WriteShortDescription(string description, LanguageWriter writer) {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}<summary>{description.CleanupXMLString()}</summary>");
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
            var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            var urlTplRef = urlTemplateVarName ?? pathParametersProp.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{prefix}new {returnType}({urlTplRef}, {requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix});");
        }
        public override string TempDictionaryVarName => "urlTplParams";
        internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters) {
            if(pathParametersType == null) return;
            writer.WriteLine($"var {TempDictionaryVarName} = new {pathParametersType.Name}({pathParametersReference});");
            if(parameters.Any())
                writer.WriteLines(parameters.Select(p =>
                    $"{TempDictionaryVarName}.Add(\"{p.Item2}\", {p.Item3});"
                ).ToArray());
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
                    _namespaceSegmentsNames = GetNamespaceNameSegments(rootNamespace).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    _namespaceSegmentsNames.Add("keyvaluepair"); //workaround as System.Collections.Generic imports keyvalue pair
                }
            }
            return _namespaceSegmentsNames;
        }
        private static IEnumerable<string> GetNamespaceNameSegments(CodeNamespace ns) {
            return (string.IsNullOrEmpty(ns.Name) ? Enumerable.Empty<string>() :
                                                    ns.Name.Split('.', StringSplitOptions.RemoveEmptyEntries))
                    .Union(ns.Namespaces.SelectMany(static x => GetNamespaceNameSegments(x)))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter writer = null)
        {
            return GetTypeString(code, targetElement, includeCollectionInformation, true);
        }
        public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation, bool includeNullableInformation)
        {
            if(code is CodeComposedTypeBase)
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            if (code is CodeType currentType) {
                var typeName = TranslateTypeAndAvoidUsingNamespaceSegmentNames(currentType, targetElement);
                var nullableSuffix = ShouldTypeHaveNullableMarker(code, typeName) && includeNullableInformation ? NullableMarkerAsString : string.Empty;
                var collectionPrefix = currentType.CollectionKind == CodeTypeCollectionKind.Complex && includeCollectionInformation ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch {
                    CodeTypeCollectionKind.Complex when includeCollectionInformation => ">",
                    CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
                    _ => string.Empty,
                };
                if (currentType.ActionOf)
                    return $"Action<{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>";
                return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
            }

            throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }
        private string TranslateTypeAndAvoidUsingNamespaceSegmentNames(CodeType currentType, CodeElement targetElement)
        {
            var parentElements = new List<string>();
            if (targetElement.Parent is CodeClass parentClass)
            {
                parentElements.AddRange(parentClass.Methods.Select(static x => x.Name).Union(parentClass.Properties.Select(static x => x.Name)));
            }
            
            var parentElementsHash = new HashSet<string>(parentElements, StringComparer.OrdinalIgnoreCase);
            var typeName = TranslateType(currentType);
            var areElementsInSameNamesSpace = DoesTypeExistsInSameNamesSpaceAsTarget(currentType, targetElement);
            if (currentType.TypeDefinition != null &&
                    (
                        GetNamesInUseByNamespaceSegments(targetElement).Contains(typeName) && !areElementsInSameNamesSpace         // match if elements are not in the same namespace and the type name is used in the namespace segments
                    ||  parentElementsHash.Contains(typeName)                                                                   // match if type name is used in the parent elements segments
                    ||  !areElementsInSameNamesSpace && DoesTypeExistsInTargetAncestorNamespace(currentType, targetElement)     // match if elements are not in the same namespace and the type exists in target ancestor namespace
                    ||  !areElementsInSameNamesSpace && DoesModelTypeExistsInMultipleImportedNamespaces(currentType, targetElement)          // match if elements is not imported already by another namespace.
                    )
                )
                return $"{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name}.{typeName}";
            return typeName;
        }

        private static bool DoesTypeExistsInSameNamesSpaceAsTarget(CodeType currentType, CodeElement targetElement)
        {
            return currentType?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>()?.Name.Equals(targetElement?.GetImmediateParentOfType<CodeNamespace>()?.Name) ?? false;
        }

        private static bool DoesTypeExistsInTargetAncestorNamespace(CodeType currentType, CodeElement targetElement)
        {
            // Avoid type ambiguity on similarly named classes. Currently, if we have namespaces A and A.B where both namespaces have type T,
            // Trying to use type A.B.T in namespace A without using a qualified name will break the build.
            // Similarly, if we have type A.B.C.D.T1 that needs to be used within type A.B.C.T2, but there's also a type
            // A.B.T1, using T1 in T2 will resolve A.B.T1 even if you have a using statement with A.B.C.D.
            var hasChildWithName = false;
            if (currentType != null && currentType.TypeDefinition != null && !currentType.IsExternal && targetElement != null)
            {
                var typeName = currentType.TypeDefinition.Name;
                var ns = targetElement.GetImmediateParentOfType<CodeNamespace>();
                var rootNs = ns?.GetRootNamespace();
                while (ns is not null && ns != rootNs && !hasChildWithName)
                {
                    hasChildWithName = ns.GetChildElements(true).OfType<CodeClass>().Any(c => c.Name?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true);
                    ns = ns.Parent is CodeNamespace n ? n : (ns.GetImmediateParentOfType<CodeNamespace>());
                }
            }
            return hasChildWithName;
        }
        
        private static bool DoesModelTypeExistsInMultipleImportedNamespaces(CodeType currentType, CodeElement targetElement)
        {
            if ( currentType.TypeDefinition is CodeClass { Parent: CodeNamespace currentTypeNamespace } codeClass)
            {
                var targetClass = targetElement.GetImmediateParentOfType<CodeClass>();
                var importedNamespaces = targetClass.StartBlock.Usings
                    .Where( codeUsing => !codeUsing.IsExternal                                                                      // 1. Are defined during generation(not external) 
                                         && !codeUsing.Name.Equals(currentTypeNamespace.Name, StringComparison.OrdinalIgnoreCase))  // 2. Do not match the namespace of the current type
                    .Select( codeUsing => codeUsing.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>())
                    .DistinctBy( declaredNamespace => declaredNamespace.Name);

                return importedNamespaces.Any(importedNamespace => importedNamespace.FindChildByName<CodeClass>(codeClass.Name,false) != null);
            }
            return false;
        }
        
        public override string TranslateType(CodeType type)
        {
            return type.Name switch
            {
                "integer" => "int",
                "boolean" => "bool",
                "int64" => "long",
                "string" or "float" or "double" or "object" or "void" or "decimal" or "sbyte" or "byte" => type.Name.ToLowerInvariant(),// little casing hack
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
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter writer = null)
        {
            var parameterType = GetTypeString(parameter.Type, targetElement);
            var defaultValue = parameter switch {
                _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
                _ when parameter.Optional => " = default",
                _ => string.Empty,
            };
            return $"{parameterType} {parameter.Name.ToFirstCharacterLowerCase()}{defaultValue}";
        }
    }
}
