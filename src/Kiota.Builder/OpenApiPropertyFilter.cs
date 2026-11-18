using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kiota.Builder.Configuration;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Kiota.Builder;

internal sealed class OpenApiPropertyFilter(ILogger logger)
{
    internal void Apply(OpenApiDocument document, GenerationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.IncludeProperties.Count == 0 && configuration.ExcludeProperties.Count == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();
        var includeSelectors = configuration.IncludeProperties.Select(ModelPropertySelector.Parse).ToHashSet();
        var excludeSelectors = configuration.ExcludeProperties.Select(ModelPropertySelector.Parse).ToHashSet();
        var components = (document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
            .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        var declarations = new Dictionary<ModelPropertySelector, List<IOpenApiSchema>>();
        var owners = new Dictionary<IOpenApiSchema, HashSet<string>>(ReferenceEqualityComparer.Instance);
        foreach (var (name, schema) in components)
            foreach (var fragment in GetOwnedFragments(schema, cancellationToken))
            {
                if (!owners.TryGetValue(fragment, out var names))
                    owners[fragment] = names = new(StringComparer.Ordinal);
                names.Add(name);
                foreach (var property in GetDeclaredProperties(fragment)?.Keys ?? [])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var selector = new ModelPropertySelector(name, property);
                    if (!declarations.TryGetValue(selector, out var fragments))
                        declarations[selector] = fragments = [];
                    fragments.Add(fragment);
                }
            }

        var supportedComponents = new HashSet<string>(StringComparer.Ordinal);
        void CheckSupportedComposition(string component, ModelPropertySelector selector)
        {
            if (!supportedComponents.Add(component)) return;
            if (GetComposedSchemas(components[component], cancellationToken).OfType<OpenApiSchemaReference>()
                .Any(static reference => reference.Reference is { Properties.Count: > 0 } or
                { AllOf.Count: > 0 } or { AnyOf.Count: > 0 } or { OneOf.Count: > 0 }))
                throw new InvalidOperationException($"Cannot apply property selection '{selector}' to component '{component}' because its composition contains a schema with properties or composition beside $ref. Use an explicit allOf with separate reference and inline entries, or omit the selector.");
        }

        HashSet<ModelPropertySelector> ResolveSelectors(IEnumerable<ModelPropertySelector> selectors)
        {
            var resolved = new HashSet<ModelPropertySelector>();
            foreach (var selector in selectors.OrderBy(static x => x.ToString(), StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!selector.Component.Contains('*', StringComparison.Ordinal) && components.ContainsKey(selector.Component))
                    CheckSupportedComposition(selector.Component, selector);
                if (selector.HasWildcard)
                {
                    var componentMatches = components.Keys.Where(ModelPropertySelector.CreateNameMatcher(selector.Component)).ToHashSet(StringComparer.Ordinal);
                    if (componentMatches.Count == 0)
                        throw new InvalidOperationException($"No components match property selector '{selector}'.");
                    var propertyMatches = ModelPropertySelector.CreateNameMatcher(selector.Property);
                    var matches = declarations.Keys.Where(x => componentMatches.Contains(x.Component) && propertyMatches(x.Property)).ToArray();
                    if (matches.Length == 0)
                        throw new InvalidOperationException($"No owned properties match property selector '{selector}'.");
                    foreach (var match in matches)
                        CheckSupportedComposition(match.Component, selector);
                    resolved.UnionWith(matches);
                    continue;
                }
                if (!components.TryGetValue(selector.Component, out var schema))
                    throw new InvalidOperationException($"Unknown component '{selector.Component}' in property selector '{selector}'.");
                if (!declarations.ContainsKey(selector))
                {
                    var declaringComponents = GetComposedSchemas(schema, cancellationToken)
                        .Where(x => GetDeclaredProperties(x)?.ContainsKey(selector.Property) == true && owners.ContainsKey(x))
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

        var includedComponents = includes.Select(static x => x.Component).ToHashSet(StringComparer.Ordinal);
        var omissions = new List<(ModelPropertySelector Selector, IOpenApiSchema Fragment)>();
        var candidates = new Dictionary<IOpenApiSchema, HashSet<string>>(ReferenceEqualityComparer.Instance);
        foreach (var (selector, fragments) in declarations.OrderBy(static x => x.Key.ToString(), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var omit = excludes.Contains(selector) || includedComponents.Contains(selector.Component) && !includes.Contains(selector);
            if (!omit) continue;
            foreach (var fragment in fragments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                omissions.Add((selector, fragment));
                if (!candidates.TryGetValue(fragment, out var names))
                    candidates[fragment] = names = new(StringComparer.Ordinal);
                names.Add(selector.Property);
            }
        }
        var protectedDeclarations = GetProtectedDeclarations(document, candidates, new ProtectionBudget(cancellationToken));
        string? GetProtectionReason(IOpenApiSchema fragment, string property) =>
            protectedDeclarations.TryGetValue(fragment, out var names) ? names.GetValueOrDefault(property) : null;

        foreach (var (selector, fragment) in omissions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (excludes.Contains(selector) && GetProtectionReason(fragment, selector.Property) is { } reason)
                throw new InvalidOperationException($"Cannot exclude {reason} property '{selector}'.");
        }

        // Commit the validated plan without cancellation checks so a canceled analysis cannot partially trim the document.
        cancellationToken.ThrowIfCancellationRequested();
        var removed = 0;
        foreach (var (selector, fragment) in omissions)
        {
            if (GetProtectionReason(fragment, selector.Property) is { } reason)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Retaining {ProtectionReason} property {PropertySelector}", reason, selector.ToString());
                continue;
            }
            if (fragment is OpenApiSchema { Properties: { } properties } schema && properties.Remove(selector.Property))
            {
                removed++;
                // Preserve the object type previously implied by its properties.
                if (properties.Count == 0)
                {
                    schema.Type ??= JsonSchemaType.Object;
                    schema.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
                    schema.Extensions[OpenApiKiotaTrimmedPropertiesExtension.Name] = OpenApiKiotaTrimmedPropertiesExtension.Instance;
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
                .Concat(GetDeclaredProperties(schema)?.Values ?? [])
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

    private static Dictionary<IOpenApiSchema, Dictionary<string, string>> GetProtectedDeclarations(
        OpenApiDocument document, Dictionary<IOpenApiSchema, HashSet<string>> candidates, ProtectionBudget budget)
    {
        var protection = new Dictionary<IOpenApiSchema, Dictionary<string, string>>(ReferenceEqualityComparer.Instance);
        if (candidates.Count == 0) return protection;
        var states = new Dictionary<IOpenApiSchema, ProtectionState>(ReferenceEqualityComparer.Instance);
        var undiscovered = new Queue<ProtectionState>();
        ProtectionState GetState(IOpenApiSchema schema)
        {
            budget.Step();
            if (!states.TryGetValue(schema, out var state))
            {
                state = new ProtectionState(schema);
                states[schema] = state;
                undiscovered.Enqueue(state);
            }
            return state;
        }
        new OpenApiWalker(new SchemaVisitor(schema => GetState(schema))).Walk(document);
        foreach (var schema in candidates.Keys)
            GetState(schema);
        while (undiscovered.TryDequeue(out var state))
        {
            // Nested values have their own entry contexts, not the containing object's composition.
            foreach (var nested in state.NestedSchemas)
                GetState(nested);
            foreach (var child in state.AllOf.Concat(state.Unions.OfType<IList<IOpenApiSchema>>().SelectMany(static x => x)))
            {
                var target = GetState(child);
                state.Children.Add(target);
                target.Parents.Add(state);
            }
        }
        HashSet<ProtectionState> Reach(IEnumerable<ProtectionState> seeds, Func<ProtectionState, IEnumerable<ProtectionState>> neighbors)
        {
            var reached = seeds.ToHashSet();
            var pending = new Queue<ProtectionState>(reached);
            while (pending.TryDequeue(out var current))
                foreach (var neighbor in neighbors(current))
                {
                    budget.Step();
                    if (reached.Add(neighbor))
                        pending.Enqueue(neighbor);
                }
            return reached;
        }
        var candidateNames = candidates.Values.SelectMany(static names => names).ToHashSet(StringComparer.Ordinal);
        if (!states.Values.Any(state => state.ProtectedNames.Any(candidateNames.Contains) ||
            state.DependentRequired?.Values.Any(candidateNames.Overlaps) == true))
            return protection;
        var declarations = candidates.Keys.Select(key => states[key]).ToArray();
        var relevant = Reach(declarations, static state => state.Children.Concat(state.Parents));
        var roots = relevant;
        if (!relevant.Any(state => state.DependentRequired?.Values.Any(candidateNames.Overlaps) == true))
        {
            // Only roots that reach a declaration can protect it. Keep paths from those
            // roots to declarations or matching constraints, without exploring other branches.
            roots = Reach(declarations, static state => state.Parents);
            var reachable = Reach(roots, static state => state.Children);
            var constraints = reachable.Where(state => state.ProtectedNames.Any(candidateNames.Contains)).ToArray();
            if (constraints.Length == 0) return protection;
            relevant = Reach(declarations.Concat(constraints), state => state.Parents.Where(reachable.Contains));
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
                budget.Step();
                ordered.Add(current);
                foreach (var child in current.Children)
                {
                    budget.Step();
                    if (relevant.Contains(child) && visited.Add(child))
                        pending.Enqueue(child);
                }
            }
        }
        foreach (var root in states.Values.Where(state => state.Parents.Count == 0 && relevant.Contains(state)))
            Visit(root);
        foreach (var state in states.Values)
            if (relevant.Contains(state))
                Visit(state);

        // Shared union occurrences use the same choice variables. Keep reachability symbolic
        // so redundant wrappers cannot mix alternatives or expand every reference path.
        var conditions = new ProtectionConditions(budget);
        // Every schema can be an entry point, including different members of a composition cycle.
        var orderedRoots = ordered.Where(roots.Contains).ToArray();
        var rootConditions = conditions.CreateChoices(orderedRoots.Length);
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
        for (var i = 0; i < orderedRoots.Length; i++)
            Activate(orderedRoots[i], rootConditions[i]);
        foreach (var state in ordered)
        {
            foreach (var child in state.AllOf)
                if (relevant.Contains(states[child]))
                    state.Edges.Add((states[child], ProtectionConditions.True));
            foreach (var union in state.Unions)
            {
                if (union is not { Count: > 0 } || !union.Any(child => relevant.Contains(states[child]))) continue;
                var choices = conditions.CreateChoices(union.Count);
                for (var i = 0; i < union.Count; i++)
                    if (relevant.Contains(states[union[i]]))
                        state.Edges.Add((states[union[i]], choices[i]));
            }
        }
        while (work.TryDequeue(out var state))
        {
            state.Queued = false;
            foreach (var (target, condition) in state.Edges)
                Activate(target, conditions.And(state.Activation, condition));
        }

        var candidatesByName = new Dictionary<string, List<ProtectionState>>(StringComparer.Ordinal);
        foreach (var (schema, properties) in candidates)
            foreach (var property in properties)
            {
                budget.Step();
                if (!candidatesByName.TryGetValue(property, out var fragments))
                    candidatesByName[property] = fragments = [];
                fragments.Add(states[schema]);
            }
        var dependencies = new Dictionary<string, List<(ProtectionState State, HashSet<string> Dependents)>>(StringComparer.Ordinal);
        foreach (var state in ordered)
        {
            if (state.DependentRequired is not { } dependentRequired) continue;
            foreach (var (trigger, dependents) in dependentRequired)
            {
                budget.Step();
                if (dependents.Count == 0) continue;
                if (!dependencies.TryGetValue(trigger, out var rules))
                    dependencies[trigger] = rules = [];
                rules.Add((state, dependents));
            }
        }

        var presence = new Dictionary<string, int>(StringComparer.Ordinal);
        var pendingTriggers = new Queue<string>();
        var queuedTriggers = new HashSet<string>(StringComparer.Ordinal);
        void Present(string name, int condition)
        {
            if (!dependencies.ContainsKey(name)) return;
            var before = presence.GetValueOrDefault(name);
            var after = conditions.Or(before, condition);
            if (before == after) return;
            presence[name] = after;
            if (queuedTriggers.Add(name))
                pendingTriggers.Enqueue(name);
        }

        var requiredConditions = new Dictionary<string, int>(StringComparer.Ordinal);
        void Require(string name, int condition, string reason)
        {
            budget.Step();
            if (condition == ProtectionConditions.False) return;
            candidatesByName.TryGetValue(name, out var fragments);
            if (fragments is null && !dependencies.ContainsKey(name)) return;
            var before = requiredConditions.GetValueOrDefault(name);
            var after = conditions.Or(before, condition);
            if (before == after) return;
            requiredConditions[name] = after;
            // A requirement makes the name present in that context even without a declaration.
            Present(name, condition);
            if (fragments is null) return;
            foreach (var fragment in fragments)
            {
                budget.Step();
                protection.TryGetValue(fragment.Schema, out var names);
                if (names?.ContainsKey(name) == true ||
                    conditions.And(condition, fragment.Activation) == ProtectionConditions.False)
                    continue;
                if (names is null)
                    protection[fragment.Schema] = names = new(StringComparer.Ordinal);
                names.Add(name, reason);
                // A retained declaration now exists everywhere its fragment is active.
                Present(name, fragment.Activation);
            }
        }

        var unconditionalRequirements = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var state in ordered)
        {
            foreach (var property in state.ProtectedNames)
            {
                budget.Step();
                if (candidatesByName.ContainsKey(property) || dependencies.ContainsKey(property))
                    unconditionalRequirements[property] = conditions.Or(unconditionalRequirements.GetValueOrDefault(property), state.Activation);
            }
            if (dependencies.Count == 0) continue;
            candidates.TryGetValue(state.Schema, out var omittedNames);
            foreach (var property in GetDeclaredProperties(state.Schema)?.Keys ?? [])
            {
                budget.Step();
                if (omittedNames?.Contains(property) != true)
                    Present(property, state.Activation);
            }
        }
        foreach (var (name, condition) in unconditionalRequirements)
            Require(name, condition, "required or discriminator");
        while (pendingTriggers.TryDequeue(out var trigger))
        {
            queuedTriggers.Remove(trigger);
            var triggerCondition = presence[trigger];
            foreach (var (state, dependents) in dependencies[trigger])
            {
                // The keyword, surviving trigger, and dependent declaration must coexist.
                var required = conditions.And(triggerCondition, state.Activation);
                if (required == ProtectionConditions.False) continue;
                foreach (var dependent in dependents)
                    Require(dependent, required, "dependentRequired");
            }
        }
        return protection;
    }

    private sealed class ProtectionState
    {
        internal ProtectionState(IOpenApiSchema schema)
        {
            Schema = schema;
            // Reference getters fall back to the target; keep only authored sibling keywords on this node.
            var reference = (schema as OpenApiSchemaReference)?.Reference;
            AllOf = (reference is null ? schema.AllOf : reference.AllOf) ?? [];
            if (schema is OpenApiSchemaReference { Target: { } target })
                AllOf = AllOf.Prepend(target);
            Unions = [reference is null ? schema.AnyOf : reference.AnyOf, reference is null ? schema.OneOf : reference.OneOf];
            var required = reference is null ? schema.Required : reference.Required;
            var discriminator = reference is null ? schema.Discriminator : reference.Discriminator;
            DependentRequired = reference is null ? schema.DependentRequired : reference.DependentRequired;
            ProtectedNames = (required ?? Enumerable.Empty<string>())
                .Concat(new[] { discriminator?.PropertyName }.OfType<string>());
            var properties = GetDeclaredProperties(schema);
            NestedSchemas = (properties?.Values ?? [])
                .Concat(new[]
                {
                    reference is null ? schema.Items : reference.Items,
                    reference is null ? schema.AdditionalProperties : reference.AdditionalProperties,
                    reference is null ? schema.Not : reference.Not,
                }.OfType<IOpenApiSchema>())
                .Concat(discriminator?.Mapping?.Values ?? []);
        }
        internal IOpenApiSchema Schema
        {
            get;
        }
        internal IDictionary<string, HashSet<string>>? DependentRequired
        {
            get;
        }
        internal IEnumerable<IOpenApiSchema> AllOf
        {
            get;
        }
        internal IList<IOpenApiSchema>?[] Unions
        {
            get;
        }
        internal IEnumerable<string> ProtectedNames
        {
            get;
        }
        internal IEnumerable<IOpenApiSchema> NestedSchemas
        {
            get;
        }
        internal List<ProtectionState> Children { get; } = [];
        internal List<(ProtectionState Target, int Condition)> Edges { get; } = [];
        internal List<ProtectionState> Parents { get; } = [];
        internal bool Queued
        {
            get; set;
        }
        internal int Activation
        {
            get; set;
        }
    }

    // Bound the analysis independently of input branching, including cached operations and fixed-point revisits.
    private sealed class ProtectionBudget(CancellationToken cancellationToken)
    {
        private const int MaxWork = 2_000_000;
        internal const int MaxConditionDepth = 256;
        private int remaining = MaxWork;

        internal void Step(int count = 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (count > remaining)
                throw new InvalidOperationException("Property-filter protection analysis exceeded its work limit.");
            remaining -= count;
        }
    }

    private sealed class ProtectionConditions(ProtectionBudget budget)
    {
        internal const int False = 0;
        internal const int True = 1;
        private readonly List<(int Variable, int Low, int High)> nodes = [(int.MaxValue, False, False), (int.MaxValue, True, True)];
        private readonly Dictionary<(int Variable, int Low, int High), int> unique = [];
        private readonly Dictionary<(bool Conjunction, int Left, int Right), int> applications = [];
        private int variableCount;

        internal int[] CreateChoices(int count)
        {
            budget.Step(count);
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
            budget.Step();
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

        private int Apply(bool conjunction, int left, int right, int depth = 0)
        {
            budget.Step();
            if (depth >= ProtectionBudget.MaxConditionDepth)
                throw new InvalidOperationException("Property-filter protection analysis exceeded its condition depth limit.");
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
                nodes[right].Variable == variable ? nodes[right].Low : right, depth + 1);
            var high = Apply(conjunction,
                nodes[left].Variable == variable ? nodes[left].High : left,
                nodes[right].Variable == variable ? nodes[right].High : right, depth + 1);
            var result = Create(variable, low, high);
            applications[key] = result;
            return result;
        }
    }

    private static IDictionary<string, IOpenApiSchema>? GetDeclaredProperties(IOpenApiSchema schema) =>
        schema is OpenApiSchemaReference reference ? reference.Reference.Properties : schema.Properties;

    private static IEnumerable<IOpenApiSchema> GetOwnedFragments(IOpenApiSchema schema, CancellationToken cancellationToken)
    {
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IOpenApiSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(current) || current is not (OpenApiSchema or OpenApiSchemaReference)) continue;
            yield return current;
            foreach (var child in GetCompositionChildren(current))
                pending.Push(child);
        }
    }

    private static IEnumerable<IOpenApiSchema> GetComposedSchemas(IOpenApiSchema schema, CancellationToken cancellationToken)
    {
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IOpenApiSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
