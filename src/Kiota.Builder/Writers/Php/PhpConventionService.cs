using System;
using System.Diagnostics;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Extensions;
using Microsoft.VisualBasic;

namespace Kiota.Builder.Writers.Php
{
    public class PhpConventionService: ILanguageConventionService
    {
        public string GetAccessModifier(AccessModifier access)
        {
            return (access) switch
            {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private"
            };
        }

        public string StreamTypeName => "StreamInterface";

        public string VoidTypeName => "void";

        public string DocCommentPrefix => " * ";

        public string PathSegmentPropertyName => "$pathSegment";

        public string CurrentPathPropertyName => "$currentPath";

        public string HttpCorePropertyName => "$httpCore";

        public string ParseNodeInterfaceName => "ParseNode";

        private string DocCommentStart = "/**";
        private string DocCommentEnd = "*/";

        public string GetTypeString(CodeTypeBase code)
        {
            return TranslateType(code.Name);
        }

        public string TranslateType(string typeName)
        {
            return (typeName) switch
            {
                "boolean" => "bool",
                "double" or "decimal" => "float",
                "integer" => "int",
                "object" or "string" or "array" or "float" or "void" => typeName,
                _ => typeName.ToFirstCharacterUpperCase()
            };
        }

        public string GetParameterSignature(CodeParameter parameter)
        {
            return $"{(parameter.Optional ? String.Empty : "?")}{GetTypeString(parameter.Type)} ${parameter.Name}";
        }
        
        private static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            if (!String.IsNullOrEmpty(description))
            {
                writer.WriteLines(DocCommentStart, 
                    RemoveInvalidDescriptionCharacters(description), 
                    DocCommentEnd);
            }
        }

        public void AddRequestBuilderBody(bool addCurrentPathProperty, string returnType, LanguageWriter writer, string suffix = default)
        {
            var currentPath = addCurrentPathProperty ? $"$this->{RemoveDollarSignFromPropertyName(CurrentPathPropertyName)} . " : string.Empty;
            writer.WriteLines($"return new {returnType}({currentPath}$this->{RemoveDollarSignFromPropertyName(PathSegmentPropertyName)}{suffix}, $this->{RemoveDollarSignFromPropertyName(HttpCorePropertyName)});");
        }

        private string RemoveDollarSignFromPropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyName.Length < 2)
            {
                throw new ArgumentException(nameof(propertyName) + " must not be null and have at least 2 characters.");
            }
            
            return propertyName.Substring(1);
        }
    }
}
