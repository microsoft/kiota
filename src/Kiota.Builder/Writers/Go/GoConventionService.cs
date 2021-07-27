using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class GoConventionService : ILanguageConventionService
    {
        public string StreamTypeName => throw new NotImplementedException();

        public string VoidTypeName => string.Empty;

        public string DocCommentPrefix => string.Empty;

        public string PathSegmentPropertyName => throw new NotImplementedException();

        public string CurrentPathPropertyName => throw new NotImplementedException();

        public string HttpCorePropertyName => throw new NotImplementedException();

        public string ParseNodeInterfaceName => "ParseNode";
        internal string DocCommentStart = "/*";
        internal string DocCommentEnd = " */";

        public string GetAccessModifier(AccessModifier access)
        {
            throw new InvalidOperationException("go uses a naming convention for access modifiers");
        }

        public string GetParameterSignature(CodeParameter parameter)
        {
            throw new NotImplementedException();
        }

        public string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var collectionPrefix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.None => string.Empty,
                    _ => "[]",
                };
                if (currentType.ActionOf)
                    return $"func (value {collectionPrefix}{typeName}) (err error)";
                else
                    return $"{collectionPrefix}{typeName}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public string TranslateType(string typeName)
        {
            return (typeName) switch {//TODO we're probably missing a bunch of type mappings
                "void" => string.Empty,
                "string" => $"*string",
                "float" => "*float32",
                "integer" => "*int32",
                "long" => "*int64",
                "double" => "*float64",
                "boolean" => "*bool",
                "guid" => "uuid.UUID",
                "datetimeoffset" => "time.Time",
                _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
            };
        }

        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
