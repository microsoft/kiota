using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class JavaConventionService : CommonLanguageConventionService
    {
        private const string InternalStreamTypeName = "InputStream";
        public override string StreamTypeName => InternalStreamTypeName;
        private const string InternalVoidTypeName = "Void";
        public override string VoidTypeName => InternalVoidTypeName;
        public override string DocCommentPrefix => " * ";
        public override string PathSegmentPropertyName => "pathSegment";
        public override string CurrentPathPropertyName => "currentPath";
        public override string HttpCorePropertyName => "httpCore";
        public override string RawUrlPropertyName => "isRawUrl";
        internal HashSet<string> PrimitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", InternalVoidTypeName, InternalStreamTypeName };
        public override string ParseNodeInterfaceName => "ParseNode";
        internal string DocCommentStart = "/**";
        internal string DocCommentEnd = " */";
        public override string GetAccessModifier(AccessModifier access)
        {
            return (access) switch {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private",
            };
        }

        public override string GetParameterSignature(CodeParameter parameter)
        {
            var nullKeyword = parameter.Optional ? "Nullable" : "Nonnull";
            var nullAnnotation = parameter.Type.IsNullable ? $"@javax.annotation.{nullKeyword} " : string.Empty;
            return $"{nullAnnotation}final {GetTypeString(parameter.Type)} {parameter.Name}";
        }

        public override string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType);
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
        public override string TranslateType(CodeType type) {
            return (type.Name) switch {//TODO we're probably missing a bunch of type mappings
                ("void" or "boolean") when !type.IsNullable => type.Name.ToFirstCharacterLowerCase(), //little casing hack
                _ => type.Name.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentStart} {RemoveInvalidDescriptionCharacters(description)} {DocCommentEnd}");
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default) {
            // because if currentPath is null it'll add "null" to the string...
            var currentPath = addCurrentPath ? $"{CurrentPathPropertyName} + " : string.Empty;
            writer.WriteLines($"return new {returnType}({currentPath}{PathSegmentPropertyName}{suffix}, {HttpCorePropertyName}, false);");
        }
    }
}
