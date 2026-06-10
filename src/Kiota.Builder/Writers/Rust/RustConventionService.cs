using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Rust;

public class RustConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "Vec<u8>";
    public override string VoidTypeName => "()";
    public override string DocCommentPrefix => "/// ";
    public override string ParseNodeInterfaceName => "ParseNode";
    public override string TempDictionaryVarName => "url_tpl_params";
    public override string GetAccessModifier(AccessModifier access) => access switch
    {
        AccessModifier.Public => "pub ",
        AccessModifier.Protected => "pub(crate) ",
        _ => string.Empty,
    };
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var paramType = GetTypeString(parameter.Type, targetElement);
        var paramName = parameter.Name.ToSnakeCase();
        return $"{paramName}: {paramType}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"Rust does not support union types directly; the union type {code.Name} should have been converted to a wrapper by the refiner");
        if (code is CodeType currentType)
        {
            var typeName = TranslateType(currentType);
            var collectionPrefix = currentType.CollectionKind switch
            {
                CodeTypeBase.CodeTypeCollectionKind.Array or
                CodeTypeBase.CodeTypeCollectionKind.Complex when includeCollectionInformation => "Vec<",
                _ => string.Empty,
            };
            var collectionSuffix = collectionPrefix.Length > 0 ? ">" : string.Empty;
            var nullablePrefix = currentType.IsNullable &&
                                 currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None
                                 ? "Option<" : string.Empty;
            var nullableSuffix = nullablePrefix.Length > 0 ? ">" : string.Empty;
            return $"{nullablePrefix}{collectionPrefix}{typeName}{collectionSuffix}{nullableSuffix}";
        }
        throw new InvalidOperationException($"type of type {code.GetType()} is not handled");
    }
    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Name.ToLowerInvariant() switch
        {
            "void" => "()",
            "string" => "String",
            "integer" or "int32" => "i32",
            "int64" or "long" => "i64",
            "float" or "float32" => "f32",
            "double" or "float64" or "decimal" => "f64",
            "byte" or "uint8" => "u8",
            "sbyte" or "int8" => "i8",
            "boolean" or "bool" => "bool",
            "guid" or "uuid" => "uuid::Uuid",
            "datetimeoffset" or "datetime" => "chrono::DateTime<chrono::FixedOffset>",
            "dateonly" => "chrono::NaiveDate",
            "timeonly" => "chrono::NaiveTime",
            "isoduration" or "timespan" or "duration" => "IsoDuration",
            "binary" or "base64" or "base64url" => "Vec<u8>",
            "object" => "serde_json::Value",
            "iparsenode" or "parsenode" => "dyn ParseNode",
            "iserializationwriter" or "serializationwriter" => "dyn SerializationWriter",
            "" or null => "serde_json::Value",
            _ => type.Name.ToFirstCharacterUpperCase(),
        };
    }
    internal static HashSet<string> PrimitiveTypes => new(StringComparer.OrdinalIgnoreCase)
    {
        "String", "bool", "i8", "u8", "i32", "i64", "f32", "f64",
        "uuid::Uuid", "chrono::DateTime<chrono::FixedOffset>", "chrono::NaiveDate", "chrono::NaiveTime", "IsoDuration",
    };
    public static bool IsPrimitiveType(string typeName) => PrimitiveTypes.Contains(typeName);
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element.Documentation is not { } documentation) return false;
        var description = element.Documentation.GetDescription(static type => type.Name.ToFirstCharacterUpperCase());
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteLine($"{DocCommentPrefix}{description.CleanupXMLString()}");
            return true;
        }
        return false;
    }
}
