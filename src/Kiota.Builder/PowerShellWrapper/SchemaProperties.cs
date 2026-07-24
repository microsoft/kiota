using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;

namespace Kiota.Builder.PowerShellWrapper;

public sealed record CmdletProperty(string OpenApiName, string PascalName, string PsTypeName, bool IsArray);

// Maps a body schema's top-level primitive properties onto cmdlet parameters. Deliberately
// shallow, per team decision: nested complex properties (assignedLicenses, employeeOrgData,
// and the like) are skipped rather than modeled. Two special cases: "id" is excluded because
// the server assigns it, and passwordProfile is flagged separately via HasPasswordProfile
// because creating a user requires it.
public static class SchemaProperties
{
    public static IReadOnlyList<CmdletProperty> ExtractPrimitiveProperties(IOpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var result = new List<CmdletProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Walk(IOpenApiSchema s)
        {
            foreach (var inherited in s.AllOf ?? [])
                Walk(inherited);

            foreach (var (name, propSchema) in s.Properties ?? new Dictionary<string, IOpenApiSchema>())
            {
                if (IsProtocolOrServerManagedProperty(name, propSchema) || !seen.Add(name))
                    continue;

                if (IsPlainScalar(propSchema))
                {
                    result.Add(new CmdletProperty(name, name.ToFirstCharacterUpperCase(), MapPsType(propSchema.Type!.Value), IsArray: false));
                }
                else if (propSchema.Type == JsonSchemaType.Array && propSchema.Items is { } items && IsPlainScalar(items))
                {
                    result.Add(new CmdletProperty(name, name.ToFirstCharacterUpperCase(), MapPsType(items.Type!.Value) + "[]", IsArray: true));
                }
            }
        }

        Walk(schema);
        return result;
    }

    // passwordProfile is a nested complex type, so ExtractPrimitiveProperties skips it, but
    // Graph requires it to create a user. This flag lets the emitter add the two flattened
    // parameters (-Password, -ForceChangePasswordNextSignIn) that make New-MgUser usable.
    public static bool HasPasswordProfile(IOpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.Properties?.ContainsKey("passwordProfile") ?? false)
            return true;
        return schema.AllOf?.Any(HasPasswordProfile) ?? false;
    }

    // A "format" on a string (date-time, uuid, byte, ...) means Kiota maps it to a non-string
    // CLR type, and an enum-valued string becomes a real enum type. Both are left out rather
    // than guessing Kiota's mapping and risking a type mismatch. Schema.Type is a flags enum
    // and nullable unions set the Null bit, so it is masked off before comparing.
    private static bool IsPlainScalar(IOpenApiSchema schema) => (schema.Type & ~JsonSchemaType.Null) switch
    {
        JsonSchemaType.String => string.IsNullOrEmpty(schema.Format) && (schema.Enum?.Count ?? 0) == 0,
        JsonSchemaType.Boolean or JsonSchemaType.Integer or JsonSchemaType.Number => true,
        _ => false,
    };

    private static string MapPsType(JsonSchemaType openApiType) => (openApiType & ~JsonSchemaType.Null) switch
    {
        JsonSchemaType.String => "string",
        JsonSchemaType.Boolean => "bool",
        JsonSchemaType.Integer or JsonSchemaType.Number => "int",
        _ => "string",
    };

    // Excludes properties a caller cannot or should not set. "id" is server-assigned.
    // "@"-prefixed names like "@odata.type" are OData control data that Kiota's serializer
    // fills in from the model type, and they are not legal C# identifiers anyway. ReadOnly is
    // the general OpenAPI signal for server-managed. Future exclusions of this kind belong here.
    private static bool IsProtocolOrServerManagedProperty(string name, IOpenApiSchema propSchema) =>
        name == "id" || name.StartsWith('@') || propSchema.ReadOnly;
}
