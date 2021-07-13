using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class RubyConventionService : ILanguageConventionService
    {
        public string SerializerFactoryPropertyName => "serializer_factory";
        // TODO: no types in ruby
        public string StreamTypeName => "stdin";
        private const string _voidTypeName = "nil";
        public string VoidTypeName => _voidTypeName;
        public string DocCommentPrefix => "## ";
        public string PathSegmentPropertyName => "path_segment";
        public string CurrentPathPropertyName => "current_path";
        public string HttpCorePropertyName => "http_core";
        // TODO: No types in ruby
        //internal HashSet<string> PrimitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", _voidTypeName, _streamTypeName };
        public string ParseNodeInterfaceName => "parse_node";
        internal string DocCommentStart = "## ";
        internal string DocCommentEnd = "## ";
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
            return $"{parameter.Name}";
        }

        
        public string GetTypeString(CodeTypeBase code)
        {
            // // TODO: NO types
            //throw new InvalidOperationException($"No ruby types");
            return string.Empty;
        }

        public string TranslateType(string typeName)
        {
            // // TODO: NO types
            // return typeName;
            return string.Empty;
        }

        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            if(!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}");
                writer.WriteLine($"# {description}");
        }
        internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "#");
        internal void AddRequestBuilderBody(LanguageWriter writer, string suffix = default) {
            // TODO: will need this for handeling indexers, which is a dotnet specific concept. This will be used by refiner, look at java for exambple of how this is done. Not needed for now, come back to this during refiner stage.
        }
    }
}
