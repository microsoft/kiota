using System;
using System.Text.Json;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers;

internal static class PrimitiveDefaultValueUtils
{
    internal static bool IsNumericType(string typeName) =>
        typeName.Equals("byte", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("double", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("float", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("int", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("int64", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("integer", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("long", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("number", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("sbyte", StringComparison.OrdinalIgnoreCase);

    internal static bool TryNormalizeBooleanLiteral(string defaultValue, out string normalizedDefaultValue)
    {
        if (bool.TryParse(defaultValue.TrimQuotes(), out var booleanDefaultValue))
        {
            normalizedDefaultValue = booleanDefaultValue ? "true" : "false";
            return true;
        }
        normalizedDefaultValue = string.Empty;
        return false;
    }

    internal static bool TryNormalizeNumericLiteral(string defaultValue, string typeName, out string normalizedDefaultValue)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(defaultValue);
            var value = jsonDocument.RootElement;
            if (value.ValueKind != JsonValueKind.Number)
            {
                normalizedDefaultValue = string.Empty;
                return false;
            }
            var isValid = typeName.ToLowerInvariant() switch
            {
                "byte" => value.TryGetByte(out _),
                "decimal" => value.TryGetDecimal(out _),
                "double" or "number" => value.TryGetDouble(out var doubleValue) && double.IsFinite(doubleValue),
                "float" => value.TryGetSingle(out var floatValue) && float.IsFinite(floatValue),
                "int" or "integer" => value.TryGetInt32(out _),
                "int64" or "long" => value.TryGetInt64(out _),
                "sbyte" => value.TryGetSByte(out _),
                _ => false,
            };
            if (isValid)
            {
                normalizedDefaultValue = value.GetRawText();
                return true;
            }
        }
        catch (JsonException)
        {
        }
        normalizedDefaultValue = string.Empty;
        return false;
    }
}
