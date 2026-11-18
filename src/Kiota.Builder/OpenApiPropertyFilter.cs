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
        var declarations = new Dictionary<ModelPropertySelector, List<IOpenApiSchema>>();
        var owners = new Dictionary<IOpenApiSchema, HashSet<string>>(ReferenceEqualityComparer.Instance);
        foreach (var (name, schema) in components)
            foreach (var fragment in GetOwnedFragments(schema))
            {
                if (!owners.TryGetValue(fragment, out var names))
                    owners[fragment] = names = new(StringComparer.Ordinal);
                names.Add(name);
                foreach (var property in GetOwnedProperties(fragment)?.Keys ?? [])
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
                        .Where(x => GetOwnedProperties(x)?.ContainsKey(selector.Property) == true && owners.ContainsKey(x))
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

        var protectedDeclarations = GetProtectedDeclarations(document, owners);

        foreach (var selector in excludes.OrderBy(static x => x.ToString(), StringComparer.Ordinal))
            if (declarations[selector].Any(fragment => IsProtected(protectedDeclarations, fragment, selector.Property)))
                throw new InvalidOperationException($"Cannot exclude required or discriminator property '{selector}'.");

        var includedComponents = includes.Select(static x => x.Component).ToHashSet(StringComparer.Ordinal);
        var removed = 0;
        foreach (var (selector, fragments) in declarations.OrderBy(static x => x.Key.ToString(), StringComparer.Ordinal))
        {
            var omit = excludes.Contains(selector) || includedComponents.Contains(selector.Component) && !includes.Contains(selector);
            if (!omit) continue;
            foreach (var fragment in fragments)
            {
                if (IsProtected(protectedDeclarations, fragment, selector.Property))
                {
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Retaining required or discriminator property {PropertySelector}", selector.ToString());
                    continue;
                }
                if (GetOwnedProperties(fragment) is { } properties && properties.Remove(selector.Property))
                {
                    removed++;
                    // Preserve the object type previously implied by its properties.
                    if (properties.Count == 0)
                        PreserveTrimmedObject(fragment);
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
            }
            foreach (var child in GetCompositionChildren(schema)
                .Concat(GetOwnedProperties(schema)?.Values ?? [])
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

    private static bool IsProtected(Dictionary<IOpenApiSchema, HashSet<string>> protection, IOpenApiSchema fragment, string property) =>
        protection.TryGetValue(fragment, out var names) && names.Contains(property);

    private static Dictionary<IOpenApiSchema, HashSet<string>> GetProtectedDeclarations(
        OpenApiDocument document, Dictionary<IOpenApiSchema, HashSet<string>> owners)
    {
        var protection = new Dictionary<IOpenApiSchema, HashSet<string>>(ReferenceEqualityComparer.Instance);
        var states = new Dictionary<IOpenApiSchema, ProtectionState>(ReferenceEqualityComparer.Instance);
        var undiscovered = new Queue<ProtectionState>();
        ProtectionState GetState(IOpenApiSchema schema)
        {
            if (!states.TryGetValue(schema, out var state))
            {
                state = new ProtectionState(schema);
                states[schema] = state;
                undiscovered.Enqueue(state);
            }
            return state;
        }
        new OpenApiWalker(new SchemaVisitor(schema => GetState(schema))).Walk(document);
        while (undiscovered.TryDequeue(out var state))
        {
            // Nested values have their own entry contexts, not the containing object's composition.
            foreach (var nested in state.NestedSchemas)
                GetState(nested);
            foreach (var child in state.AllOf.Concat(state.Unions.OfType<IList<IOpenApiSchema>>().SelectMany(static x => x)))
            {
                var target = GetState(child);
                state.Children.Add(target);
                target.HasParent = true;
            }
        }

        var ordered = new List<ProtectionState>();
        var visited = new HashSet<ProtectionState>();
        void Visit(ProtectionState root)
        {
            var pending = new Queue<ProtectionState>();
            if (visited.Add(root))
                pending.Enqueue(root);
            while (pending.TryDequeue(out var current))
            {
                ordered.Add(current);
                foreach (var child in current.Children)
                    if (visited.Add(child))
                        pending.Enqueue(child);
            }
        }
        foreach (var root in states.Values.Where(static x => !x.HasParent))
            Visit(root);
        foreach (var state in states.Values)
            Visit(state);

        // Shared union occurrences use the same choice variables. Keep reachability symbolic
        // so redundant wrappers cannot mix alternatives or expand every reference path.
        var conditions = new ProtectionConditions();
        // Every schema can be an entry point, including different members of a composition cycle.
        var rootConditions = conditions.CreateChoices(ordered.Count);
        var work = new Queue<ProtectionState>();
        void Activate(ProtectionState state, int condition)
        {
            var activation = conditions.Or(state.Activation, condition);
            if (activation == state.Activation) return;
            state.Activation = activation;
            if (!state.Queued)
            {
                state.Queued = true;
                work.Enqueue(state);
            }
        }
        for (var i = 0; i < ordered.Count; i++)
            Activate(ordered[i], rootConditions[i]);
        foreach (var state in ordered)
        {
            foreach (var child in state.AllOf)
                state.Edges.Add((GetState(child), ProtectionConditions.True));
            foreach (var union in state.Unions)
            {
                if (union is not { Count: > 0 }) continue;
                var choices = conditions.CreateChoices(union.Count);
                for (var i = 0; i < union.Count; i++)
                    state.Edges.Add((GetState(union[i]), choices[i]));
            }
        }
        while (work.TryDequeue(out var state))
        {
            state.Queued = false;
            foreach (var (target, condition) in state.Edges)
                Activate(target, conditions.And(state.Activation, condition));
        }

        var requiredConditions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var state in states.Values)
            foreach (var property in state.ProtectedNames)
                requiredConditions[property] = conditions.Or(requiredConditions.GetValueOrDefault(property), state.Activation);
        foreach (var schema in owners.Keys)
            foreach (var property in GetOwnedProperties(schema)?.Keys ?? [])
                if (requiredConditions.TryGetValue(property, out var required) &&
                    conditions.And(required, states[schema].Activation) != ProtectionConditions.False)
                {
                    if (!protection.TryGetValue(schema, out var names))
                        protection[schema] = names = new(StringComparer.Ordinal);
                    names.Add(property);
                }
        return protection;
    }

    private sealed class ProtectionState
    {
        internal ProtectionState(IOpenApiSchema schema)
        {
            // Reference getters fall back to the target; keep only authored sibling keywords on this node.
            var reference = (schema as OpenApiSchemaReference)?.Reference;
            AllOf = (reference is null ? schema.AllOf : reference.AllOf)?.ToArray() ?? [];
            if (schema is OpenApiSchemaReference { Target: { } target })
                AllOf = [target, .. AllOf];
            Unions = [reference is null ? schema.AnyOf : reference.AnyOf, reference is null ? schema.OneOf : reference.OneOf];
            var required = reference is null ? schema.Required : reference.Required;
            var discriminator = reference is null ? schema.Discriminator : reference.Discriminator;
            ProtectedNames = (required ?? Enumerable.Empty<string>())
                .Concat(new[] { discriminator?.PropertyName }.OfType<string>()).ToArray();
            var properties = reference is null ? schema.Properties : reference.Properties;
            NestedSchemas = (properties?.Values ?? [])
                .Concat(new[]
                {
                    reference is null ? schema.Items : reference.Items,
                    reference is null ? schema.AdditionalProperties : reference.AdditionalProperties,
                    reference is null ? schema.Not : reference.Not,
                }.OfType<IOpenApiSchema>())
                .Concat(discriminator?.Mapping?.Values ?? []).ToArray();
        }
        internal IOpenApiSchema[] AllOf
        {
            get;
        }
        internal IList<IOpenApiSchema>?[] Unions
        {
            get;
        }
        internal string[] ProtectedNames
        {
            get;
        }
        internal IOpenApiSchema[] NestedSchemas
        {
            get;
        }
        internal List<ProtectionState> Children { get; } = [];
        internal List<(ProtectionState Target, int Condition)> Edges { get; } = [];
        internal bool HasParent
        {
            get; set;
        }
        internal bool Queued
        {
            get; set;
        }
        internal int Activation
        {
            get; set;
        }
    }

    private sealed class ProtectionConditions
    {
        internal const int False = 0;
        internal const int True = 1;
        private readonly List<(int Variable, int Low, int High)> nodes = [(int.MaxValue, False, False), (int.MaxValue, True, True)];
        private readonly Dictionary<(int Variable, int Low, int High), int> unique = [];
        private readonly Dictionary<(bool Conjunction, int Left, int Right), int> applications = [];
        private int variableCount;

        internal int[] CreateChoices(int count)
        {
            var bits = 0;
            while ((1L << bits) < count)
                bits++;
            var firstVariable = variableCount;
            variableCount += bits;
            var result = new int[count];
            for (var choice = 0; choice < count; choice++)
            {
                var condition = True;
                for (var bit = bits - 1; bit >= 0; bit--)
                    condition = (choice & (1L << bit)) == 0
                        ? Create(firstVariable + bit, condition, False)
                        : Create(firstVariable + bit, False, condition);
                result[choice] = condition;
            }
            return result;
        }

        private int Create(int variable, int low, int high)
        {
            if (low == high) return low;
            var key = (variable, low, high);
            if (unique.TryGetValue(key, out var existing)) return existing;
            var index = nodes.Count;
            nodes.Add(key);
            unique[key] = index;
            return index;
        }

        internal int And(int left, int right) => Apply(true, left, right);
        internal int Or(int left, int right) => Apply(false, left, right);

        private int Apply(bool conjunction, int left, int right)
        {
            if (left == right) return left;
            if (left > right) (left, right) = (right, left);
            if (conjunction && left == False) return False;
            if (conjunction && left == True || !conjunction && left == False) return right;
            if (!conjunction && (left == True || right == True)) return True;
            var key = (conjunction, left, right);
            if (applications.TryGetValue(key, out var existing)) return existing;
            var variable = Math.Min(nodes[left].Variable, nodes[right].Variable);
            var low = Apply(conjunction,
                nodes[left].Variable == variable ? nodes[left].Low : left,
                nodes[right].Variable == variable ? nodes[right].Low : right);
            var high = Apply(conjunction,
                nodes[left].Variable == variable ? nodes[left].High : left,
                nodes[right].Variable == variable ? nodes[right].High : right);
            var result = Create(variable, low, high);
            applications[key] = result;
            return result;
        }
    }

    private static IDictionary<string, IOpenApiSchema>? GetOwnedProperties(IOpenApiSchema schema) =>
        schema is OpenApiSchemaReference reference ? reference.Reference.Properties : schema.Properties;

    private static void PreserveTrimmedObject(IOpenApiSchema fragment)
    {
        IDictionary<string, IOpenApiExtension> extensions;
        switch (fragment)
        {
            case OpenApiSchemaReference reference:
                reference.Type ??= JsonSchemaType.Object;
                // The convenience getter may expose the target's dictionary; annotate a local copy instead.
                extensions = reference.Reference.Extensions ??= reference.Extensions is { } inheritedExtensions
                    ? new Dictionary<string, IOpenApiExtension>(inheritedExtensions, StringComparer.Ordinal)
                    : new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
                break;
            case OpenApiSchema schema:
                schema.Type ??= JsonSchemaType.Object;
                extensions = schema.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
                break;
            default:
                throw new InvalidOperationException($"Unsupported owned schema type '{fragment.GetType().Name}'.");
        }
        extensions[OpenApiKiotaTrimmedPropertiesExtension.Name] = OpenApiKiotaTrimmedPropertiesExtension.Instance;
    }

    private static IEnumerable<IOpenApiSchema> GetOwnedFragments(IOpenApiSchema schema)
    {
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IOpenApiSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current) || current is not (OpenApiSchema or OpenApiSchemaReference)) continue;
            yield return current;
            foreach (var child in GetCompositionChildren(current))
                pending.Push(child);
        }
    }

    private static IEnumerable<IOpenApiSchema> GetComposedSchemas(IOpenApiSchema schema)
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
            }
            yield return current;
            foreach (var child in GetCompositionChildren(current))
                pending.Push(child);
        }
    }

    private static IEnumerable<IOpenApiSchema> GetCompositionChildren(IOpenApiSchema schema)
    {
        var reference = (schema as OpenApiSchemaReference)?.Reference;
        var allOf = reference is null ? schema.AllOf : reference.AllOf;
        var anyOf = reference is null ? schema.AnyOf : reference.AnyOf;
        var oneOf = reference is null ? schema.OneOf : reference.OneOf;
        return (allOf ?? []).Concat(anyOf ?? []).Concat(oneOf ?? []);
    }

    private sealed class SchemaVisitor(Action<IOpenApiSchema> visit) : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiSchema schema) => visit(schema);
        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            if (referenceHolder is IOpenApiSchema schema)
                visit(schema);
        }
    }
}
