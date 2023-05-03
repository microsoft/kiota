using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;
public class MissingDiscriminator : ValidationRule<OpenApiDocument>
{
    public MissingDiscriminator(GenerationConfiguration configuration) : base((context, document) =>
    {
        var idx = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        document.InitializeInheritanceIndex(idx);
        if (document.Components != null)
            Parallel.ForEach(document.Components.Schemas, entry =>
            {
                ValidateSchema(entry.Value, context, idx, entry.Key);
            });
        var inlineSchemasToValidate = document.Paths
                                        ?.SelectMany(static x => x.Value.Operations.Values.Select(y => (x.Key, Operation: y)))
                                        .SelectMany(x => x.Operation.GetResponseSchemas(OpenApiOperationExtensions.SuccessCodes, configuration.StructuredMimeTypes).Select(y => (x.Key, Schema: y)))
                                        .Where(static x => string.IsNullOrEmpty(x.Schema.Reference?.Id))
                                        .ToArray() ?? Array.Empty<(string, OpenApiSchema)>();
        Parallel.ForEach(inlineSchemasToValidate, entry =>
        {
            ValidateSchema(entry.Schema, context, idx, entry.Key);
        });
    })
    {
    }
    private static void ValidateSchema(OpenApiSchema schema, IValidationContext context, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> idx, string address)
    {
        if (!schema.IsInclusiveUnion() && !schema.IsExclusiveUnion())
            return;
        if (schema.AnyOf.All(static x => !x.IsObject()) && schema.OneOf.All(static x => !x.IsObject()))
            return;
        if (string.IsNullOrEmpty(schema.GetDiscriminatorPropertyName()) || !schema.GetDiscriminatorMappings(idx).Any())
            context.CreateWarning(nameof(MissingDiscriminator), $"The schema {address} is a polymorphic type but does not define a discriminator. This will result in a serialization errors.");
    }
}
