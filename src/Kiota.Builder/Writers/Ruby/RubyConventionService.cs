using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class RubyConventionService : ILanguageConventionService
    {
        public string SerializerFactoryPropertyName => "serializer_factory";
        // TODO: no types in ruby
        private const string _streamTypeName = "stdin";
        public string StreamTypeName => _streamTypeName;
        private const string _voidTypeName = "nil";
        public string VoidTypeName => _voidTypeName;
        public string DocCommentPrefix => " # ";
        public string PathSegmentPropertyName => "path_segment";
        public string CurrentPathPropertyName => "current_path";
        public string HttpCorePropertyName => "http_core";
        // TODO: No types in ruby
        //internal HashSet<string> PrimitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", _voidTypeName, _streamTypeName };
        public string ParseNodeInterfaceName => "parse_node";
        internal string DocCommentStart = "=begin";
        internal string DocCommentEnd = " =end";
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
            return $"@{parameter.Name}";
        }

        
        public string GetTypeString(CodeTypeBase code)
        {
            // TODO: NO types
            throw new InvalidOperationException($"No ruby types");
        }

        public string TranslateType(string typeName)
        {
            // TODO: NO types
            return typeName;
        }

        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            //TODO
        }
        internal void AddRequestBuilderBody(string returnType, LanguageWriter writer, string suffix = default) {
            // TODO
        }
    }
}
