using System;
using System.Collections.Concurrent;
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

    private static IEnumerable<string> GetAllProperties(string prefix, OpenApiSchema schema)
    {
        var fullPrefix = string.IsNullOrEmpty(prefix) ? "" : prefix + ".";
        var inlinedProperties = schema.Properties
                .Concat(schema.AllOf.SelectMany(static x => x.Properties))
                .Concat(schema.AnyOf.SelectMany(static x => x.Properties))
                .Concat(schema.OneOf.SelectMany(static x => x.Properties));

        var currentProps = inlinedProperties.Select(p => fullPrefix + p.Key);
        var nestedProps = inlinedProperties.SelectMany(p => GetAllProperties(fullPrefix + p.Key, p.Value));
        return currentProps.Concat(nestedProps);
    }
    private static void ValidateSchema(OpenApiSchema schema, IValidationContext context)
    {
        var allProperties = GetAllProperties("", schema);
        if (allProperties.Count() != allProperties.Distinct().Count())
        {
            context.CreateWarning(nameof(UnsupportedInheritanceSemantics), $"The schema {schema.GetSchemaName()} is using inheritance and one or more fields is overwritten.");
        }
    }
}
