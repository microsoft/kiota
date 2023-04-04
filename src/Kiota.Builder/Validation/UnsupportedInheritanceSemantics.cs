using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;
public class UnsupportedInheritanceSemantics : ValidationRule<OpenApiDocument>
{

    private static readonly PropertyOpenApiSchemaComparer propertySchemaComparer = new();
    public UnsupportedInheritanceSemantics(GenerationConfiguration configuration) : base((context, document) =>
    {
        if (document.Components != null)
            Parallel.ForEach(document.Components.Schemas, entry =>
            {
                ValidateSchema(entry.Value, context);
            });
        var inlineSchemasToValidate = document.Paths
                                ?.SelectMany(static x => x.Value.Operations.Values.Select(y => (x.Key, Operation: y)))
                                .SelectMany(x => x.Operation.GetResponseSchemas(OpenApiOperationExtensions.SuccessCodes, configuration.StructuredMimeTypes).Select(static y => y))
                                .Where(static x => string.IsNullOrEmpty(x.Reference?.Id))
                                .ToArray() ?? Array.Empty<OpenApiSchema>();
        Parallel.ForEach(inlineSchemasToValidate, entry =>
        {
            ValidateSchema(entry, context);
        });
    })
    {
    }
    private static string GetPrefix(string prefix, string key)
    {
        return string.IsNullOrEmpty(prefix) ? key : prefix + "." + key;
    }
    private static IEnumerable<(string, OpenApiSchema)> GetAllProperties(string prefix, OpenApiSchema schema)
    {
        var inlinedProperties = schema.Properties
                .Concat(schema.AllOf.SelectMany(static x => x.Properties))
                .Concat(schema.AnyOf.SelectMany(static x => x.Properties))
                .Concat(schema.OneOf.SelectMany(static x => x.Properties));

        var currentProps = inlinedProperties.Select(p => (GetPrefix(prefix, p.Key), p.Value));
        var nestedProps = inlinedProperties.SelectMany(p => GetAllProperties(GetPrefix(prefix, p.Key), p.Value));
        return currentProps.Concat(nestedProps);
    }
    private static void ValidateSchema(OpenApiSchema schema, IValidationContext context)
    {
        var allProperties = GetAllProperties(string.Empty, schema);

        var divergingInheritance = false;
        foreach (var prop in allProperties)
        {
            if (allProperties
                .Where(x => x.Item1.Equals(prop.Item1, StringComparison.Ordinal))
                .GroupBy(static x => x, propertySchemaComparer)
                .Count() > 1)
            {
                divergingInheritance = true;
                break;
            }
        }

        if (divergingInheritance)
        {
            context.CreateWarning(nameof(UnsupportedInheritanceSemantics), $"The schema {schema.GetSchemaName()} is using inheritance and one or more fields is overwritten with an incompatible type.");
        }
    }
}
