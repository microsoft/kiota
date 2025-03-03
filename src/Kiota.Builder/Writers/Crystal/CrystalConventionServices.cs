using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using static Kiota.Builder.CodeDOM.CodeTypeBase;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using Kiota.Builder.Extensions;
using System.Linq;
using System.Globalization;

namespace Kiota.Builder.Writers.Crystal
{
    public class CrystalConventionService : CommonLanguageConventionService
    {
        private static readonly Regex PascalCaseRegex = new Regex(@"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", RegexOptions.Compiled);

        public override string StreamTypeName => "IO::Memory";
        public override string VoidTypeName => "Nil";
        public override string DocCommentPrefix => "# ";
        public override string TempDictionaryVarName => "temp_dict";
        public override string ParseNodeInterfaceName => "ParseNode";
        private static readonly HashSet<string> NullableTypes = new(StringComparer.OrdinalIgnoreCase) { "Int32", "Bool", "Float32", "Float64", "Int64", "String", "Char", "Symbol" };

        public bool IsPrimitiveType(string typeName)
        {
            return typeName switch
            {
                "Int32" => true,
                "Int64" => true,
                "Float32" => true,
                "Float64" => true,
                "Bool" => true,
                "String" => true,
                "Char" => true,
                "Symbol" => true,
                _ => false,
            };
        }


        public override string GetAccessModifier(AccessModifier access)
        {
            return access switch
            {
                AccessModifier.Public => "public",
                AccessModifier.Protected => "protected",
                AccessModifier.Private => "private",
                _ => throw new ArgumentOutOfRangeException(nameof(access), access, null)
            };
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            return ToSnakeCase(parameter.Name);
        }

        public string GetPropertyName(CodeProperty property)
        {
            ArgumentNullException.ThrowIfNull(property);
            return ToSnakeCase(property.Name);
        }

        public  string GetMethodName(string originalName)
        {
            ArgumentNullException.ThrowIfNull(originalName);
            return ToSnakeCase(originalName);
        }

        public  string GetClassName(string originalName)
        {
            ArgumentNullException.ThrowIfNull(originalName);
            return ToPascalCase(originalName);
        }

        public bool IsAbstract(CodeMethod method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return method.IsOfKind(CodeMethodKind.Factory);
        }


        public string GetTypeStringForDocumentation(CodeTypeBase code, CodeElement targetElement)
        {
            if (code is CodeComposedTypeBase composedType)
                return GetComposedTypeTypeString(composedType, targetElement, false);
            else if (code is CodeType currentType)
            {
                var typeName = TranslateType(currentType);
                var nullableSuffix = ShouldTypeHaveNullableMarker(currentType, typeName) ? " | Nil" : string.Empty;
                return $"{typeName}{nullableSuffix}";
            }

            throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
        }

        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
        {
            if (code is CodeComposedTypeBase composedType)
                return GetComposedTypeTypeString(composedType, targetElement, includeCollectionInformation);
            else if (code is CodeType currentType)
            {
                var typeName = TranslateType(currentType);
                var nullableSuffix = ShouldTypeHaveNullableMarker(currentType, typeName) ? " | Nil" : string.Empty;
                var collectionPrefix = currentType.CollectionKind == CodeTypeCollectionKind.Complex && includeCollectionInformation ? "Array(" : string.Empty;
                var collectionSuffix = currentType.CollectionKind switch
                {
                    CodeTypeCollectionKind.Complex when includeCollectionInformation => ")",
                    CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
                    _ => string.Empty,
                };
                return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
            }

            throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
        }

        public override string TranslateType(CodeType type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type.TypeDefinition != null)
                return type.TypeDefinition.Name;

            return type.Name switch
            {
                "integer" => "Int32",
                "boolean" => "Bool",
                "int64" => "Int64",
                "string" or "float" or "double" or "object" or "void" or "decimal" or "sbyte" or "byte" => type.Name.ToFirstCharacterUpperCase(),
                "binary" or "base64" or "base64url" => "Bytes",
                _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "Object",
            };
        }

        private static bool ShouldTypeHaveNullableMarker(CodeTypeBase propType, string propTypeName)
        {
            return propType.IsNullable && NullableTypes.Contains(propTypeName);
        }

        private string ToSnakeCase(string input)
        {
            return PascalCaseRegex.Replace(input, "_$1").ToLower(CultureInfo.InvariantCulture);
        }

        private string ToPascalCase(string input)
        {
            return char.ToUpper(input[0], CultureInfo.InvariantCulture) + input.Substring(1);
        }

        private string GetComposedTypeTypeString(CodeComposedTypeBase composedType, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if (!composedType.Types.Any())
                throw new InvalidOperationException("Composed type should be comprised of at least one type");

            var typesDelimiter = composedType is CodeUnionType ? " | " :
                throw new InvalidOperationException("Unknown composed type");

            return string.Join(typesDelimiter, composedType.Types.Select(x => GetTypeString(x, targetElement, includeCollectionInformation)));
        }
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = null, string? prefix = null, IEnumerable<CodeParameter>? pathParameters = null, bool includeIndent = true)
        {
            if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
                parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
            {
                var pathParametersSuffix = pathParameters == null || !pathParameters.Any() ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => x.Name.ToSnakeCase()))}";
                var urlTplRef = string.IsNullOrEmpty(urlTemplateVarName) ? pathParametersProp.Name.ToSnakeCase() : urlTemplateVarName;
                writer.WriteLine($"{prefix}{returnType}.new({urlTplRef}, {requestAdapterProp.Name.ToSnakeCase()}{pathParametersSuffix})", includeIndent);
            }
        }
        public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(element);
            if (element.Documentation is not { } documentation) return false;
            if (element is not CodeElement codeElement) return false;
            if (documentation.DescriptionAvailable)
            {
                var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type, codeElement), normalizationFunc: static x => x.CleanupXMLString());
                writer.WriteLine($"{DocCommentPrefix}{prefix}{description}{suffix}");
                return true;
            }
            return false;
        }

        public bool WriteLongDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(element);
            if (element.Documentation is not { } documentation) return false;
            if (element is not CodeElement codeElement) return false;
            if (documentation.DescriptionAvailable || documentation.ExternalDocumentationAvailable)
            {
                writer.WriteLine($"{DocCommentPrefix}{prefix}<summary>");
                if (documentation.DescriptionAvailable)
                {
                    var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type, codeElement), normalizationFunc: static x => x.CleanupXMLString());
                    writer.WriteLine($"{DocCommentPrefix}{description}");
                }
                if (documentation.ExternalDocumentationAvailable)
                    writer.WriteLine($"{DocCommentPrefix}{documentation.DocumentationLabel} <see href=\"{documentation.DocumentationLink}\" />");
                writer.WriteLine($"{DocCommentPrefix}</summary>{suffix}");
                return true;
            }
            return false;
        }

        public void WriteAdditionalDescriptionItem(string description, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            if (!string.IsNullOrEmpty(description))
                writer.WriteLine($"{DocCommentPrefix}{description}");
        }

        public void WriteDeprecationAttribute(IDeprecableElement element, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(element);
            
            if (element is IDocumentedElement documentedElement && 
                documentedElement.Documentation?.DescriptionAvailable == true)
            {
                var codeElement = element as CodeElement;
                if (codeElement != null)
                {
                    var description = documentedElement.Documentation.GetDescription(
                        type => GetTypeStringForDocumentation(type, codeElement), 
                        normalizationFunc: static x => x.CleanupXMLString());
                    writer.WriteLine($"{DocCommentPrefix}# DEPRECATED: {description}");
                }
            }
        }

        internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, string varName = "", params (CodeTypeBase, string, string)[] parameters)
        {
            if (pathParametersType == null) return;
            if (string.IsNullOrEmpty(varName))
            {
                varName = "url_tpl_params";
                writer.WriteLine($"{varName} = Hash(String, String).new({pathParametersReference})");
            }
            if (parameters.Length != 0)
            {
                foreach (var (ct, name, identName) in parameters)
                {
                    string nullCheck = string.Empty;
                    if (ct.CollectionKind == CodeTypeCollectionKind.None && ct.IsNullable)
                    {
                        if (nameof(String).Equals(ct.Name, StringComparison.OrdinalIgnoreCase))
                            nullCheck = $"unless {identName}.nil? || {identName}.strip.empty?";
                        else
                            nullCheck = $"unless {identName}.nil?";
                    }
                    writer.WriteLine($"{nullCheck} {varName}[\"{name}\"] = {identName}; end");
                }
            }
        }
    }
}
