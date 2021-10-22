using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeTypeBase;

namespace Kiota.Builder.Writers.TypeScript {
    public class TypeScriptConventionService : CommonLanguageConventionService
    {
        public TypeScriptConventionService(LanguageWriter languageWriter)
        {
            writer = languageWriter;
        }
        private readonly LanguageWriter writer;
        public override string StreamTypeName => "ReadableStream";

        public override string VoidTypeName => throw new System.NotImplementedException();

        public override string DocCommentPrefix => " * ";
        public override string ParseNodeInterfaceName => "ParseNode";
        internal string DocCommentStart = "/**";
        internal string DocCommentEnd = " */";
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, IEnumerable<CodeParameter> pathParameters = default) {
            var codePathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var urlTemplateParams = urlTemplateVarName ?? $"this.{pathParametersProperty.Name}";
            writer.WriteLines($"return new {returnType}({urlTemplateParams}, this.{requestAdapterProp.Name}{codePathParametersSuffix});");
        }
        public override string TempDictionaryVarName => "urlTplParams";
        internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters) {
            if(pathParametersType == null) return;
            writer.WriteLine($"const {TempDictionaryVarName} = getPathParameters({pathParametersReference});");
            if(parameters.Any())
                writer.WriteLines(parameters.Select(p => {
                    var stringSuffix = p.Item1.Name.Equals("string", StringComparison.OrdinalIgnoreCase) ? string.Empty : ".toString()";
                    return $"{p.Item3} && {TempDictionaryVarName}.set(\"{p.Item2}\", {p.Item3}{stringSuffix});";
                }).ToArray());
        }
        #pragma warning restore CA1822 // Method should be static
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
            var defaultValueSuffiix = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" = {parameter.DefaultValue}";
            return $"{parameter.Name.ToFirstCharacterLowerCase()}{(parameter.Optional && parameter.Type.IsNullable ? "?" : string.Empty)}: {GetTypeString(parameter.Type, targetElement)}{(parameter.Type.IsNullable ? " | undefined": string.Empty)}{defaultValueSuffiix}";
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true) {
            if(code is null)
                return null;
            var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None && includeCollectionInformation ? string.Empty : "[]";
            if(code is CodeUnionType currentUnion && currentUnion.Types.Any())
                return currentUnion.Types.Select(x => GetTypeString(x, targetElement)).Aggregate((x, y) => $"{x} | {y}") + collectionSuffix;
            else if(code is CodeType currentType) {
                var typeName = GetTypeAlias(currentType, targetElement) ?? TranslateType(currentType);
                if (code.ActionOf)
                    return WriteInlineDeclaration(currentType, targetElement);
                else
                    return $"{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }
        private static string GetTypeAlias(CodeType targetType, CodeElement targetElement) {
            var parentClass = targetElement.GetImmediateParentOfType<CodeClass>();
            if(parentClass != null && parentClass.StartBlock is CodeClass.Declaration currentDeclaration) {
                var aliasedUsing = currentDeclaration.Usings
                                                    .FirstOrDefault(x => !x.IsExternal &&
                                                                    x.Declaration.TypeDefinition == targetType.TypeDefinition &&
                                                                    !string.IsNullOrEmpty(x.Alias));
                return aliasedUsing?.Alias;
            }
            return null;
        }
        private string WriteInlineDeclaration(CodeType currentType, CodeElement targetElement) {
            writer.IncreaseIndent(4);
            var childElements = (currentType?.TypeDefinition as CodeClass)
                                        ?.Properties
                                        ?.OrderBy(x => x.Name)
                                        ?.Select(x => $"{x.Name}?: {GetTypeString(x.Type, targetElement)}");
            var innerDeclaration = childElements?.Any() ?? false ? 
                                            LanguageWriter.NewLine +
                                            writer.GetIndent() +
                                            childElements
                                            .Aggregate((x, y) => $"{x};{LanguageWriter.NewLine}{writer.GetIndent()}{y}")
                                            .Replace(';', ',') +
                                            LanguageWriter.NewLine +
                                            writer.GetIndent()
                                        : string.Empty;
            writer.DecreaseIndent();
            if(string.IsNullOrEmpty(innerDeclaration))
                return "object";
            else
                return $"{{{innerDeclaration}}}";
        }

        public override string TranslateType(CodeType type)
        {
            return type.Name switch  {
                "integer" or "int64" or "float" or "double" => "number",
                "binary" => "string",
                "DateTimeOffset" => "Date",
                "String" or "Object" or "Boolean" or "Void" or "string" or "object" or "boolean" or "void" => type.Name.ToFirstCharacterLowerCase(), // little casing hack
                _ => type.Name.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        #pragma warning disable CA1822 // Method should be static
        public bool IsPrimitiveType(string typeName) {
            return typeName switch {
                "number" or "string" or "byte[]" or "boolean" or "void" => true,
                _ => false,
            };
        }
        #pragma warning restore CA1822 // Method should be static
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)} {DocCommentEnd}");
        }
    }
}
