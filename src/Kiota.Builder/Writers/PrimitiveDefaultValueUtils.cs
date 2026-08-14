using System;
using System.Text.Json;

namespace Kiota.Builder.Writers;

internal static class PrimitiveDefaultValueUtils
{
    internal static bool TryNormalizeNumericLiteral(string defaultValue, string typeName, out string normalizedDefaultValue)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(defaultValue);
            var value = jsonDocument.RootElement;
            var isValid = typeName.ToLowerInvariant() switch
            {
                "byte" => value.TryGetByte(out _),
                "decimal" => value.TryGetDecimal(out _),
                "double" => value.TryGetDouble(out _),
                "float" => value.TryGetSingle(out _),
                "int" or "integer" => value.TryGetInt32(out _),
                "int64" => value.TryGetInt64(out _),
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
