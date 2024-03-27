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

public class MissingDiscriminator : ValidationRule<OpenApiDocument>
{
    public MissingDiscriminator(GenerationConfiguration configuration) : base(nameof(MissingDiscriminator), (context, document) =>
    {
        var idx = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        document.InitializeInheritanceIndex(idx);
        if (document.Components != null)
            Parallel.ForEach(document.Components.Schemas, entry =>
            {
                ValidateSchemaRecursively(entry.Value, context, idx, entry.Key);
            });
        var inlineSchemasToValidate = document.Paths
                                        ?.SelectMany(static x => x.Value.Operations.Values.Select(y => (x.Key, Operation: y)))
                                        .SelectMany(x => x.Operation.GetResponseSchemas(OpenApiOperationExtensions.SuccessCodes, configuration.StructuredMimeTypes).Select(y => (x.Key, Schema: y)))
                                        .Where(static x => string.IsNullOrEmpty(x.Schema.Reference?.Id))
                                        .ToArray() ?? Array.Empty<(string, OpenApiSchema)>();
        Parallel.ForEach(inlineSchemasToValidate, entry =>
        {
            ValidateSchemaRecursively(entry.Schema, context, idx, entry.Key);
        });
    })
    {
    }

    private static void ValidateSchemaRecursively(OpenApiSchema schema, IValidationContext context, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> idx, string address)
    {
        foreach (var property in schema.Properties ?? Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>())
        {
            ValidateSchemaRecursively(property.Value, context, idx, $"{address}.Properties.{property.Key}");
        }

        foreach (var allOfSchema in schema.AllOf)
        {
            ValidateSchemaRecursively(allOfSchema, context, idx, $"{address}.AllOf");
        }

        foreach (var oneOfSchema in schema.OneOf)
        {
            ValidateSchemaRecursively(oneOfSchema, context, idx, $"{address}.OneOf");
        }

        foreach (var anyOfSchema in schema.AnyOf)
        {
            ValidateSchemaRecursively(anyOfSchema, context, idx, $"{address}.AnyOf");
        }

        if (schema.Items != null)
        {
            ValidateSchemaRecursively(schema.Items, context, idx, $"{address}.Items");
        }

        ValidateSchema(schema, context, idx, address);
    }

    private static void ValidateSchema(OpenApiSchema schema, IValidationContext context, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> idx, string address)
    {
        if (!schema.IsInclusiveUnion() && !schema.IsExclusiveUnion())
            return;
        if (schema.AnyOf.All(static x => x.IsScalar()) && schema.OneOf.All(static x => x.IsScalar()))
            return;
        if (string.IsNullOrEmpty(schema.GetDiscriminatorPropertyName()) || !schema.GetDiscriminatorMappings(idx).Any())
            context.CreateWarning(nameof(MissingDiscriminator), $"The schema {address} is a polymorphic type but does not define a discriminator. This will result in a serialization errors.");
    }
}
