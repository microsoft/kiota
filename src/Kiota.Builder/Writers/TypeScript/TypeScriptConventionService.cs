using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class TypeScriptConventionService : ILanguageConventionService
    {
        public TypeScriptConventionService(LanguageWriter languageWriter)
        {
            writer = languageWriter;
        }
        private readonly LanguageWriter writer;
        public string StreamTypeName => "ReadableStream";

        public string VoidTypeName => throw new System.NotImplementedException();

        public string DocCommentPrefix => " * ";

        public string PathSegmentPropertyName => "pathSegment";

        public string CurrentPathPropertyName => "currentPath";

        public string HttpCorePropertyName => "httpCore";

        public string ParseNodeInterfaceName => "ParseNode";

        public object RawUrlPropertyName => "isRawUrl";

        internal string DocCommentStart = "/**";
        internal string DocCommentEnd = " */";
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default) {
            var currentPath = addCurrentPath ? $"this.{CurrentPathPropertyName} + " : string.Empty;
            writer.WriteLines($"return new {returnType}({currentPath}this.{PathSegmentPropertyName}{suffix}, this.{HttpCorePropertyName}, false);");
        }

        public string GetAccessModifier(AccessModifier access)
        {
            return access switch {
                AccessModifier.Public => "public",
                AccessModifier.Protected => "protected",
                _ => "private",
            };
        }

        public string GetParameterSignature(CodeParameter parameter)
        {
            var defaultValueSuffiix = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" = {parameter.DefaultValue}";
            return $"{parameter.Name}{(parameter.Optional && parameter.Type.IsNullable ? "?" : string.Empty)}: {GetTypeString(parameter.Type)}{(parameter.Type.IsNullable ? " | undefined": string.Empty)}{defaultValueSuffiix}";
        }

        public string GetTypeString(CodeTypeBase code)
        {
            var collectionSuffix = code.CollectionKind == CodeType.CodeTypeCollectionKind.None ? string.Empty : "[]";
            if(code is CodeUnionType currentUnion && currentUnion.Types.Any())
                return currentUnion.Types.Select(x => GetTypeString(x)).Aggregate((x, y) => $"{x} | {y}") + collectionSuffix;
            else if(code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                if (code.ActionOf)
                    return WriteInlineDeclaration(currentType);
                else
                    return $"{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }
        private string WriteInlineDeclaration(CodeType currentType) {
            writer.IncreaseIndent(4);
            var childElements = (currentType?.TypeDefinition as CodeClass)
                                        ?.GetChildElements(true)
                                        ?.OfType<CodeProperty>()
                                        ?.OrderBy(x => x.Name)
                                        ?.Select(x => $"{x.Name}?: {GetTypeString(x.Type)}");
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

        public string TranslateType(string typeName)
        {
            return (typeName) switch  {//TODO we're probably missing a bunch of type mappings
                "integer" => "number",
                "double" => "number",
                "string" or "object" or "boolean" or "void" => typeName, // little casing hack
                _ => typeName.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        public bool IsPrimitiveType(string typeName) {
            return typeName switch {
                ("number" or "string" or "byte[]" or "boolean" or "void") => true,
                _ => false,
            };
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)} {DocCommentEnd}");
        }
    }
}
