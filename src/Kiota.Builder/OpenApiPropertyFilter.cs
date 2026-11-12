using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Configuration;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Kiota.Builder;

internal sealed class OpenApiPropertyFilter(ILogger logger)
{
    internal void Apply(OpenApiDocument document, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.IncludeProperties.Count == 0 && configuration.ExcludeProperties.Count == 0)
            return;

        var includeSelectors = configuration.IncludeProperties.Select(ModelPropertySelector.Parse).ToHashSet();
        var excludeSelectors = configuration.ExcludeProperties.Select(ModelPropertySelector.Parse).ToHashSet();
        var components = (document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
            .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        var declarations = new Dictionary<ModelPropertySelector, List<OpenApiSchema>>();
        var owners = new Dictionary<IOpenApiSchema, HashSet<string>>(ReferenceEqualityComparer.Instance);
        foreach (var (name, schema) in components)
            foreach (var fragment in GetOwnedFragments(schema))
            {
                if (!owners.TryGetValue(fragment, out var names))
                    owners[fragment] = names = new(StringComparer.Ordinal);
                names.Add(name);
                foreach (var property in fragment.Properties?.Keys ?? [])
                {
                    var selector = new ModelPropertySelector(name, property);
                    if (!declarations.TryGetValue(selector, out var fragments))
                        declarations[selector] = fragments = [];
                    fragments.Add(fragment);
                }
            }

        HashSet<ModelPropertySelector> ResolveSelectors(IEnumerable<ModelPropertySelector> selectors)
        {
            var resolved = new HashSet<ModelPropertySelector>();
            foreach (var selector in selectors.OrderBy(static x => x.ToString(), StringComparer.Ordinal))
            {
                if (selector.HasWildcard)
                {
                    var componentMatches = components.Keys.Where(ModelPropertySelector.CreateNameMatcher(selector.Component)).ToHashSet(StringComparer.Ordinal);
                    if (componentMatches.Count == 0)
                        throw new InvalidOperationException($"No components match property selector '{selector}'.");
                    var propertyMatches = ModelPropertySelector.CreateNameMatcher(selector.Property);
                    var matches = declarations.Keys.Where(x => componentMatches.Contains(x.Component) && propertyMatches(x.Property)).ToArray();
                    if (matches.Length == 0)
                        throw new InvalidOperationException($"No owned properties match property selector '{selector}'.");
                    resolved.UnionWith(matches);
                    continue;
                }
                if (!components.TryGetValue(selector.Component, out var schema))
                    throw new InvalidOperationException($"Unknown component '{selector.Component}' in property selector '{selector}'.");
                if (!declarations.ContainsKey(selector))
                {
                    var declaringComponents = GetComposedSchemas(schema)
                        .Where(x => x.Properties?.ContainsKey(selector.Property) == true && owners.ContainsKey(x))
                        .SelectMany(x => owners[x])
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray();
                    var hint = declaringComponents.Length == 0 ? string.Empty :
                        $" Select the property on its declaring component: {string.Join(", ", declaringComponents)}.";
                    throw new InvalidOperationException($"Unknown owned property '{selector.Property}' on component '{selector.Component}'.{hint}");
                }
                resolved.Add(selector);
            }
            return resolved;
        }
        var includes = ResolveSelectors(includeSelectors);
        var excludes = ResolveSelectors(excludeSelectors);

        var protectedProperties = new HashSet<ModelPropertySelector>();
        var protectionVisited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var protectionPath = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        void ProtectSchema(IOpenApiSchema schema, IOpenApiSchema[] ancestors)
        {
            if ((ancestors.Length == 0 && !protectionVisited.Add(schema)) || !protectionPath.Add(schema)) return;
            try
            {
                var fragments = GetComposedSchemas(schema, includeUnions: false).ToArray();
                var protectedNames = fragments.SelectMany(static x => x.Required ?? Enumerable.Empty<string>())
                    .Concat(fragments.Select(static x => x.Discriminator?.PropertyName).OfType<string>())
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var fragment in GetComposedSchemas(schema).Concat(ancestors).Distinct<IOpenApiSchema>(ReferenceEqualityComparer.Instance).Where(owners.ContainsKey))
                    foreach (var name in owners[fragment])
                        foreach (var property in fragment.Properties?.Keys.Where(protectedNames.Contains) ?? [])
                            protectedProperties.Add(new(name, property));
                // Branch constraints can name shared ancestor fields, but not sibling-only declarations.
                var sharedDeclarations = ancestors.Concat(fragments).Distinct<IOpenApiSchema>(ReferenceEqualityComparer.Instance).ToArray();
                foreach (var branch in fragments.SelectMany(static x => (x.AnyOf ?? []).Concat(x.OneOf ?? [])))
                    ProtectSchema(branch, sharedDeclarations);
            }
            finally
            {
                protectionPath.Remove(schema);
            }
        }
        new OpenApiWalker(new SchemaVisitor(schema => ProtectSchema(schema, []))).Walk(document);

        foreach (var selector in excludes.OrderBy(static x => x.ToString(), StringComparer.Ordinal))
            if (protectedProperties.Contains(selector))
                throw new InvalidOperationException($"Cannot exclude required or discriminator property '{selector}'.");

        var includedComponents = includes.Select(static x => x.Component).ToHashSet(StringComparer.Ordinal);
        var removed = 0;
        foreach (var (selector, fragments) in declarations.OrderBy(static x => x.Key.ToString(), StringComparer.Ordinal))
        {
            var omit = excludes.Contains(selector) || includedComponents.Contains(selector.Component) && !includes.Contains(selector);
            if (!omit) continue;
            if (protectedProperties.Contains(selector))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Retaining required or discriminator property {PropertySelector}", selector.ToString());
                continue;
            }
            foreach (var fragment in fragments)
                if (fragment.Properties is { } properties && properties.Remove(selector.Property))
                {
                    removed++;
                    // Preserve the object type previously implied by its properties.
                    if (properties.Count == 0)
                    {
                        fragment.Type ??= JsonSchemaType.Object;
                        fragment.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
                        fragment.Extensions[OpenApiKiotaTrimmedPropertiesExtension.Name] = OpenApiKiotaTrimmedPropertiesExtension.Instance;
                    }
                }
        }
        if (!logger.IsEnabled(LogLevel.Information)) return;
        logger.LogInformation("Removed {PropertyCount} schema property declarations", removed);

        var usedComponents = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        void VisitUsedSchema(IOpenApiSchema schema)
        {
            if (!visited.Add(schema)) return;
            if (owners.TryGetValue(schema, out var names))
                usedComponents.UnionWith(names);
            if (schema is OpenApiSchemaReference reference)
            {
                if (reference.Target is { } target)
                    VisitUsedSchema(target);
                return;
            }
            foreach (var child in GetCompositionChildren(schema)
                .Concat(schema.Properties?.Values ?? [])
                .Concat(new[] { schema.Items, schema.AdditionalProperties, schema.Not }.OfType<IOpenApiSchema>())
                .Concat(schema.Discriminator?.Mapping?.Values ?? []))
                VisitUsedSchema(child);
        }
        var visitedPaths = new HashSet<IOpenApiPathItem>(ReferenceEqualityComparer.Instance);
        void VisitContent(IDictionary<string, IOpenApiMediaType>? content)
        {
            foreach (var media in content?.Values ?? [])
                if (media.Schema is { } schema)
                    VisitUsedSchema(schema);
        }
        void VisitParameters(IEnumerable<IOpenApiParameter>? parameters)
        {
            foreach (var parameter in parameters ?? [])
            {
                if (parameter.Schema is { } schema)
                    VisitUsedSchema(schema);
                VisitContent(parameter.Content);
            }
        }
        void VisitPath(IOpenApiPathItem path)
        {
            if (!visitedPaths.Add(path)) return;
            VisitParameters(path.Parameters);
            foreach (var operation in path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
            {
                VisitParameters(operation.Parameters);
                VisitContent(operation.RequestBody?.Content);
                foreach (var response in operation.Responses?.Values ?? Enumerable.Empty<IOpenApiResponse>())
                {
                    VisitContent(response.Content);
                    foreach (var header in response.Headers?.Values ?? [])
                    {
                        if (header.Schema is { } schema)
                            VisitUsedSchema(schema);
                        VisitContent(header.Content);
                    }
                }
                foreach (var callback in operation.Callbacks?.Values ?? Enumerable.Empty<IOpenApiCallback>())
                    foreach (var callbackPath in callback.PathItems?.Values ?? Enumerable.Empty<IOpenApiPathItem>())
                        VisitPath(callbackPath);
            }
        }
        foreach (var path in (document.Paths?.Values ?? Enumerable.Empty<IOpenApiPathItem>()).Concat(document.Webhooks?.Values ?? []))
            VisitPath(path);
        foreach (var component in includes.Union(excludes).Select(static x => x.Component).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            if (!usedComponents.Contains(component) && logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Property selection targets component {Component} not referenced by the selected operations", component);
    }

    private static IEnumerable<OpenApiSchema> GetOwnedFragments(IOpenApiSchema schema)
    {
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IOpenApiSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current) || current is not OpenApiSchema fragment) continue;
            yield return fragment;
            foreach (var child in GetCompositionChildren(fragment))
                pending.Push(child);
        }
    }

    private static IEnumerable<IOpenApiSchema> GetComposedSchemas(IOpenApiSchema schema, bool includeUnions = true)
    {
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IOpenApiSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            if (current is OpenApiSchemaReference reference)
            {
                if (reference.Target is { } target)
                    pending.Push(target);
                continue;
            }
            yield return current;
            foreach (var child in includeUnions ? GetCompositionChildren(current) : current.AllOf ?? [])
                pending.Push(child);
        }
    }

    private static IEnumerable<IOpenApiSchema> GetCompositionChildren(IOpenApiSchema schema) =>
        (schema.AllOf ?? []).Concat(schema.AnyOf ?? []).Concat(schema.OneOf ?? []);

    private sealed class SchemaVisitor(Action<IOpenApiSchema> visit) : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiSchema schema) => visit(schema);
    }
}
