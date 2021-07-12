using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class JavaConventionService : ILanguageConventionService
    {
        private const string _streamTypeName = "InputStream";
        public string StreamTypeName => _streamTypeName;
        private const string _voidTypeName = "Void";
        public string VoidTypeName => _voidTypeName;
        public string DocCommentPrefix => " * ";
        public string PathSegmentPropertyName => "pathSegment";
        public string CurrentPathPropertyName => "currentPath";
        public string HttpCorePropertyName => "httpCore";
        internal HashSet<string> PrimitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", _voidTypeName, _streamTypeName };
        public string ParseNodeInterfaceName => "ParseNode";
        internal string DocCommentStart = "/**";
        internal string DocCommentEnd = " */";
        public string GetAccessModifier(AccessModifier access)
        {
            return (access) switch {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private",
            };
        }

        public string GetParameterSignature(CodeParameter parameter)
        {
            return $"@javax.annotation.{(parameter.Optional ? "Nullable" : "Nonnull")} final {GetTypeString(parameter.Type)} {parameter.Name}";
        }

        public string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.Complex => ">",
                    CodeType.CodeTypeCollectionKind.Array => "[]",
                    _ => string.Empty,
                };
                if (currentType.ActionOf)
                    return $"java.util.function.Consumer<{collectionPrefix}{typeName}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public string TranslateType(string typeName)
        {
            return (typeName) switch {//TODO we're probably missing a bunch of type mappings
                ("void") => typeName.ToFirstCharacterLowerCase(), //little casing hack
                _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
            };
        }

        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)} {DocCommentEnd}");
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default) {
            // because if currentPath is null it'll add "null" to the string...
            var currentPath = addCurrentPath ? $"{CurrentPathPropertyName} + " : string.Empty;
            writer.WriteLines($"return new {returnType}({currentPath}{PathSegmentPropertyName}{suffix}, {HttpCorePropertyName});");
        }
    }
}
