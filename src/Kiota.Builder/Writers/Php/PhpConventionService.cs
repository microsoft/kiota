using System;
using Kiota.Builder.Extensions;

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

        public string DocCommentStart = "/**";
        public string DocCommentEnd = "*/";

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
                writer.WriteLine(DocCommentStart);
                writer.WriteLine(
                    $"{DocCommentPrefix}{RemoveInvalidDescriptionCharacters(description)}");
                writer.WriteLine(DocCommentEnd);
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

        public void WritePhpDocumentStart(LanguageWriter writer)
        {
            writer.WriteLine("<?php");
            writer.WriteLine("");
        }

        public void WriteCodeBlockEnd(LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        
        /**
         * For Php strings, having double quotes around strings might cause an issue
         * if the string contains valid variable name.
         * For example $variable = "$value" will try too set the value of
         * $variable to the variable named $value rather than the string '$value'
         * around quotes as expected.
         */
        public string ReplaceDoubleQuoteWithSingleQuote(string current)
        {
            if (string.IsNullOrEmpty(current))
            {
                return current;
            }
            if (current.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                return current.Replace('\"', '\'');
            }
            return current;
        }
    }
}
