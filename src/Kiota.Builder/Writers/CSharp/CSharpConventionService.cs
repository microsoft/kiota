using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.CSharp;

public class CSharpConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "stream";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => "/// ";
    private static readonly HashSet<string> NullableTypes = new(StringComparer.OrdinalIgnoreCase) { "int", "integer", "bool", "boolean", "float", "double", "decimal", "long", "Guid", "DateTimeOffset", "TimeSpan", "Date", "Time", "sbyte", "byte" };
    public const char NullableMarker = '?';
    public static string NullableMarkerAsString => "?";
    public override string ParseNodeInterfaceName => "IParseNode";
    public const string NullableEnableDirective = "#nullable enable";
    public const string NullableRestoreDirective = "#nullable restore";

    public const string CS0618 = "CS0618";
    public const string CS1591 = "CS1591";

    public static void WriteNullableOpening(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER", false);
        writer.WriteLine(NullableEnableDirective, false);
    }
    public static void WriteNullableMiddle(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine(NullableRestoreDirective, false);
        writer.WriteLine("#else", false);
    }
    public static void WriteNullableClosing(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine("#endif", false);
    }
    public void WritePragmaDisable(LanguageWriter writer, string code)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"#pragma warning disable {code}");
    }
    public void WritePragmaRestore(LanguageWriter writer, string code)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"#pragma warning restore {code}");
    }
    private const string ReferenceTypePrefix = "<see cref=\"";
    private const string ReferenceTypeSuffix = "\"/>";
#pragma warning disable S1006 // Method overrides should not change parameter defaults
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "<summary>", string suffix = "</summary>")
#pragma warning restore S1006 // Method overrides should not change parameter defaults
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element is not CodeElement codeElement) return false;
        if (!element.Documentation.DescriptionAvailable) return false;
        var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type, codeElement), normalizationFunc: static x => x.CleanupXMLString());
        writer.WriteLine($"{DocCommentPrefix}{prefix}{description}{suffix}");
        return true;
    }
    public void WriteAdditionalDescriptionItem(string description, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(description);
        writer.WriteLine($"{DocCommentPrefix}{description}");
    }
    public bool WriteLongDescription(IDocumentedElement element, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element.Documentation is not { } documentation) return false;
        if (element is not CodeElement codeElement) return false;
        if (documentation.DescriptionAvailable || documentation.ExternalDocumentationAvailable)
        {
            writer.WriteLine($"{DocCommentPrefix}<summary>");
            if (documentation.DescriptionAvailable)
            {
                var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type, codeElement), normalizationFunc: static x => x.CleanupXMLString());
                writer.WriteLine($"{DocCommentPrefix}{description}");
            }
            if (documentation.ExternalDocumentationAvailable)
                writer.WriteLine($"{DocCommentPrefix}{documentation.DocumentationLabel} <see href=\"{documentation.DocumentationLink}\" />");
            writer.WriteLine($"{DocCommentPrefix}</summary>");
            return true;
        }
        return false;
    }
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Internal => "internal",
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private",
        };
    }
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, string? prefix = default, IEnumerable<CodeParameter>? pathParameters = default, bool includeIndent = true)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
        {
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            var urlTplRef = string.IsNullOrEmpty(urlTemplateVarName) ? pathParametersProp.Name.ToFirstCharacterUpperCase() : urlTemplateVarName;
            writer.WriteLine($"{prefix}new {returnType}({urlTplRef}, {requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix});", includeIndent);
        }
    }
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, string varName = "", params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        if (string.IsNullOrEmpty(varName))
        {
            varName = TempDictionaryVarName;
            writer.WriteLine($"var {varName} = new {pathParametersType.Name}({pathParametersReference});");
        }
        if (parameters.Length != 0)
        {
            writer.WriteLines(parameters.Select(p =>
            {
                var (ct, name, identName) = p;
                string nullCheck = string.Empty;
                if (ct.CollectionKind == CodeTypeCollectionKind.None && ct.IsNullable)
                {
                    if (nameof(String).Equals(ct.Name, StringComparison.OrdinalIgnoreCase))
                        nullCheck = $"if (!string.IsNullOrWhiteSpace({identName})) ";
                    else
                        nullCheck = $"if ({identName} != null) ";
                }
                return $"{nullCheck}{varName}.Add(\"{name}\", {identName});";
            }).ToArray());
        }
    }
#pragma warning restore CA1822 // Method should be static
    private static bool ShouldTypeHaveNullableMarker(CodeTypeBase propType, string propTypeName)
    {
        return propType.IsNullable && (NullableTypes.Contains(propTypeName) || (propType is CodeType codeType && codeType.TypeDefinition is CodeEnum));
    }
    private HashSet<string> _namespaceSegmentsNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _namespaceSegmentsNamesLock = new();
    private HashSet<string> GetNamesInUseByNamespaceSegments(CodeElement currentElement)
    {
        if (_namespaceSegmentsNames.Count == 0)
        {
            lock (_namespaceSegmentsNamesLock)
            {
                var rootNamespace = currentElement.GetImmediateParentOfType<CodeNamespace>().GetRootNamespace();
                _namespaceSegmentsNames = GetAllNamespaces(rootNamespace)
                                            .Where(static x => !string.IsNullOrEmpty(x.Name))
                                            .SelectMany(static ns => ns.Name.Split('.', StringSplitOptions.RemoveEmptyEntries))
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                _namespaceSegmentsNames.Add("keyvaluepair"); //workaround as System.Collections.Generic imports keyvalue pair
            }
        }
        return _namespaceSegmentsNames;
    }
    private static IEnumerable<CodeNamespace> GetAllNamespaces(CodeNamespace ns)
    {
        foreach (var childNs in ns.Namespaces)
        {
            yield return childNs;
            foreach (var childNsSegment in GetAllNamespaces(childNs))
                yield return childNsSegment;
        }
    }
    public string GetTypeStringForDocumentation(CodeTypeBase code, CodeElement targetElement)
    {
        var typeString = GetTypeString(code, targetElement, true, false);// dont include nullable markers
        if (typeString.EndsWith('>'))
            return typeString.CleanupXMLString(); // don't generate cref links for generic types as concrete types generate invalid links

        return $"{ReferenceTypePrefix}{typeString.CleanupXMLString()}{ReferenceTypeSuffix}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        return GetTypeString(code, targetElement, includeCollectionInformation, true);
    }
    public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation, bool includeNullableInformation, bool includeActionInformation = true)
    {
        ArgumentNullException.ThrowIfNull(targetElement);
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var typeName = TranslateType(currentType);
            var nullableSuffix = ShouldTypeHaveNullableMarker(code, typeName) && includeNullableInformation ? NullableMarkerAsString : string.Empty;
            var collectionPrefix = currentType.CollectionKind == CodeTypeCollectionKind.Complex && includeCollectionInformation ? "List<" : string.Empty;
            var collectionSuffix = currentType.CollectionKind switch
            {
                CodeTypeCollectionKind.Complex when includeCollectionInformation => ">",
                CodeTypeCollectionKind.Array when includeCollectionInformation => "[]",
                _ => string.Empty,
            };
            var genericParameters = currentType.GenericTypeParameterValues.Any() ?
                $"<{string.Join(", ", currentType.GenericTypeParameterValues.Select(x => GetTypeString(x, targetElement, includeCollectionInformation)))}>" :
                string.Empty;
            if (currentType.ActionOf && includeActionInformation)
                return $"Action<{collectionPrefix}{typeName}{genericParameters}{nullableSuffix}{collectionSuffix}>";
            return $"{collectionPrefix}{typeName}{genericParameters}{nullableSuffix}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
    }

    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.TypeDefinition is ITypeDefinition typeDefinition)
            return typeDefinition.GetFullName();

        return type.Name switch
        {
            "integer" => "int",
            "boolean" => "bool",
            "int64" => "long",
            "string" or "float" or "double" or "object" or "void" or "decimal" or "sbyte" or "byte" => type.Name.ToLowerInvariant(),// little casing hack
            "binary" or "base64" or "base64url" => "byte[]",
            _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }
    public bool IsPrimitiveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return false;
        typeName = typeName.StripArraySuffix().TrimEnd('?').ToLowerInvariant();
        return typeName switch
        {
            "string" => true,
            _ when NullableTypes.Contains(typeName) => true,
            _ => false,
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var parameterType = GetTypeString(parameter.Type, targetElement);
        var defaultValue = parameter switch
        {
            _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
            _ when nameof(String).Equals(parameterType, StringComparison.OrdinalIgnoreCase) && parameter.Optional => " = \"\"",
            _ when parameter.Optional => " = default",
            _ => string.Empty,
        };
        return $"{parameterType} {parameter.Name.ToFirstCharacterLowerCase()}{defaultValue}";
    }
    private string GetDeprecationInformation(IDeprecableElement element)
    {
        if (element.Deprecation is null || !element.Deprecation.IsDeprecated) return string.Empty;

        var versionComment = string.IsNullOrEmpty(element.Deprecation.Version) ? string.Empty : $" as of {element.Deprecation.Version}";
        var dateComment = element.Deprecation.Date is null ? string.Empty : $" on {element.Deprecation.Date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var removalComment = element.Deprecation.RemovalDate is null ? string.Empty : $" and will be removed {element.Deprecation.RemovalDate.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        return $"[Obsolete(\"{element.Deprecation.GetDescription(type => GetTypeString(type, (element as CodeElement)!).Split('.', StringSplitOptions.TrimEntries)[^1])}{versionComment}{dateComment}{removalComment}\")]";
    }
    internal void WriteDeprecationAttribute(IDeprecableElement element, LanguageWriter writer)
    {
        var deprecationMessage = GetDeprecationInformation(element);
        if (!string.IsNullOrEmpty(deprecationMessage))
            writer.WriteLine(deprecationMessage);
    }
}
