using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Java {
    public class JavaConventionService : CommonLanguageConventionService
    {
        private const string InternalStreamTypeName = "InputStream";
        public override string StreamTypeName => InternalStreamTypeName;
        private const string InternalVoidTypeName = "Void";
        public override string VoidTypeName => InternalVoidTypeName;
        public override string DocCommentPrefix => " * ";
        internal HashSet<string> PrimitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", InternalVoidTypeName, InternalStreamTypeName };
        public override string ParseNodeInterfaceName => "ParseNode";
        internal string DocCommentStart = "/**";
        internal string DocCommentEnd = " */";
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
            var nullKeyword = parameter.Optional ? "Nullable" : "Nonnull";
            var nullAnnotation = parameter.Type.IsNullable ? $"@javax.annotation.{nullKeyword} " : string.Empty;
            return $"{nullAnnotation}final {GetTypeString(parameter.Type, targetElement)} {parameter.Name.ToFirstCharacterLowerCase()}";
        }

        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType);
                if(!currentType.IsExternal && IsSymbolDuplicated(typeName, targetElement))
                    typeName = $"{currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name}.{typeName}";

                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex && includeCollectionInformation ? "java.util.List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.Complex when includeCollectionInformation => ">",
                    CodeType.CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
                    _ => string.Empty,
                };
                if (currentType.ActionOf)
                    return $"java.util.function.Consumer<{collectionPrefix}{typeName}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }
        private static readonly CodeUsingDeclarationNameComparer usingDeclarationComparer = new();
        private static bool IsSymbolDuplicated(string symbol, CodeElement targetElement) {
            var targetClass = targetElement as CodeClass ?? targetElement.GetImmediateParentOfType<CodeClass>();
            if (targetClass.Parent is CodeClass parentClass) 
                targetClass = parentClass;
            return (targetClass.StartBlock as CodeClass.Declaration)
                            ?.Usings
                            ?.Where(x => !x.IsExternal && symbol.Equals(x.Declaration.TypeDefinition.Name, StringComparison.OrdinalIgnoreCase))
                            ?.Distinct(usingDeclarationComparer)
                            ?.Count() > 1;
        }
        public override string TranslateType(CodeType type) {
            return type.Name switch {
                "int64" => "Long",
                "void" or "boolean" when !type.IsNullable => type.Name, //little casing hack
                "binary" => "byte[]",
                _ => type.Name.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)} {DocCommentEnd}");
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, IEnumerable<CodeParameter> pathParameters = default) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var urlTemplateParams = urlTemplateVarName ?? pathParametersProperty.Name;
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            writer.WriteLines($"return new {returnType}({urlTemplateParams}, {requestAdapterProp.Name}{pathParametersSuffix});");
        }
        public override string TempDictionaryVarName => "urlTplParams";
        internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters) {
            if(pathParametersType == null) return;
            var mapTypeName = pathParametersType.Name;
            writer.WriteLine($"var {TempDictionaryVarName} = new {mapTypeName}({pathParametersReference});");
            if(parameters.Any())
                writer.WriteLines(parameters.Select(p => {
                    var stringSuffix = p.Item1.Name.Equals("string", StringComparison.OrdinalIgnoreCase) ? string.Empty : ".toString()";
                    return $"{TempDictionaryVarName}.put(\"{p.Item2}\", {p.Item3}{stringSuffix});";
                }).ToArray());
        }
        #pragma warning restore CA1822 // Method should be static
    }
}
