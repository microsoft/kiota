using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

#nullable enable

namespace Kiota.Builder.Tests;

public sealed class OpenApiPropertyFilterOracleTests : IDisposable
{
    private readonly HttpClient httpClient = new();

    public static IEnumerable<object[]> GeneratedCases =>
        Enumerable.Range(0, 64).Select(static seed => new object[] { $"generated:{seed}" });

    [Theory]
    [InlineData("shared-wrappers")]
    [InlineData("partial-overlap")]
    [InlineData("constrained-reference")]
    [InlineData("cyclic-entry")]
    [InlineData("reference-siblings")]
    [InlineData("three-alternatives")]
    [MemberData(nameof(GeneratedCases))]
    public async Task MatchesExhaustiveBranchChoiceOracleAsync(string caseName)
    {
        var graph = CreateGraph(caseName);
        var expected = FindProtectedDeclarations(graph);
        var declarations = Enumerable.Range(0, graph.Length).Where(i => graph[i].DeclaresId).ToArray();
        var document = await ReadGraphAsync(graph);
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = declarations.Select(static i => $"N{i}#keep").ToHashSet(StringComparer.Ordinal),
        }, TestContext.Current.CancellationToken);
        foreach (var declaration in declarations)
            Assert.True(expected.Contains(declaration) == document.Components!.Schemas![$"N{declaration}"].Properties!.ContainsKey("id"),
                $"{caseName}: inclusion protection differs for N{declaration}#id.");

        foreach (var declaration in declarations)
        {
            document = await ReadGraphAsync(graph);
            var configuration = new GenerationConfiguration { ExcludeProperties = [$"N{declaration}#id"] };
            if (expected.Contains(declaration))
            {
                Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration, TestContext.Current.CancellationToken));
                Assert.All(declarations, i => Assert.Contains("id", document.Components!.Schemas![$"N{i}"].Properties!.Keys));
            }
            else
            {
                new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration, TestContext.Current.CancellationToken);
                Assert.DoesNotContain("id", document.Components!.Schemas![$"N{declaration}"].Properties!.Keys);
            }
        }
    }

    // Enumerate concrete choices and walk each entry point. This deliberately does not
    // share the production decision graph, activation conditions, or protection helpers.
    private static HashSet<int> FindProtectedDeclarations(Node[] graph)
    {
        var unions = graph.SelectMany((node, index) => new[] { node.AnyOf, node.OneOf }
            .OfType<int[]>().Where(static branches => branches.Length > 0)
            .Select(branches => (Owner: index, Branches: branches))).ToArray();
        var combinations = unions.Aggregate(1, static (count, union) => checked(count * union.Branches.Length));
        var protection = new HashSet<int>();
        for (var assignment = 0; assignment < combinations; assignment++)
        {
            var choices = new List<int>[graph.Length];
            for (var i = 0; i < graph.Length; i++)
                choices[i] = [];
            var remaining = assignment;
            foreach (var union in unions)
            {
                choices[union.Owner].Add(union.Branches[remaining % union.Branches.Length]);
                remaining /= union.Branches.Length;
            }
            for (var root = 0; root < graph.Length; root++)
            {
                var active = new HashSet<int>();
                var pending = new Stack<int>();
                pending.Push(root);
                while (pending.TryPop(out var current))
                {
                    if (!active.Add(current)) continue;
                    foreach (var child in (graph[current].AllOf ?? []).Concat(choices[current]))
                        pending.Push(child);
                    if (graph[current].ReferenceTarget is { } target)
                        pending.Push(target);
                }
                if (active.Any(i => graph[i].RequiresId || graph[i].Discriminator))
                    protection.UnionWith(active.Where(i => graph[i].DeclaresId));
            }
        }
        return protection;
    }

    private async Task<OpenApiDocument> ReadGraphAsync(Node[] graph)
    {
        var schemas = new Dictionary<string, object>(StringComparer.Ordinal);
        for (var i = 0; i < graph.Length; i++)
        {
            var node = graph[i];
            var schema = new Dictionary<string, object>(StringComparer.Ordinal);
            if (node.ReferenceTarget is { } target && node.AllOf is not { Length: > 0 } &&
                node.AnyOf is not { Length: > 0 } && node.OneOf is not { Length: > 0 })
                schema["$ref"] = $"#/components/schemas/N{target}";
            if (node.DeclaresId)
                schema["properties"] = new
                {
                    id = new
                    {
                        type = "string"
                    },
                    keep = new
                    {
                        type = "string"
                    },
                };
            if (node.RequiresId)
                schema["required"] = new[] { "id" };
            if (node.Discriminator)
                schema["discriminator"] = new { propertyName = "id" };
            var conjuncts = node.ReferenceTarget is { } conjunct && !schema.ContainsKey("$ref")
                ? (node.AllOf ?? []).Prepend(conjunct).ToArray()
                : node.AllOf;
            foreach (var (keyword, children) in new[] { ("allOf", conjuncts), ("anyOf", node.AnyOf), ("oneOf", node.OneOf) })
                if (children is { Length: > 0 })
                    schema[keyword] = children.Select(static child =>
                        new Dictionary<string, string> { ["$ref"] = $"#/components/schemas/N{child}" }).ToArray();
            schemas[$"N{i}"] = schema;
        }
        await using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new
        {
            openapi = "3.1.0",
            info = new
            {
                title = "Exhaustive protection oracle",
                version = "1.0.0"
            },
            paths = new
            {
            },
            components = new
            {
                schemas
            },
        }));
        return Assert.IsType<OpenApiDocument>(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration(), httpClient).CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken));
    }

    private static Node[] CreateGraph(string name) => name switch
    {
        "shared-wrappers" =>
        [
            new(DeclaresId: true, RequiresId: true), new(DeclaresId: true),
            new(AnyOf: [0, 1]), new(AllOf: [4, 5]), new(AnyOf: [2]), new(OneOf: [2]),
        ],
        "partial-overlap" =>
        [
            new(DeclaresId: true), new(DeclaresId: true), new(RequiresId: true),
            new(AnyOf: [2, 0]), new(OneOf: [0, 1]), new(AllOf: [3, 4]),
        ],
        "constrained-reference" =>
        [
            new(DeclaresId: true), new(DeclaresId: true), new(AnyOf: [0, 1]),
            new(ReferenceTarget: 2, Discriminator: true), new(RequiresId: true),
            new(OneOf: [3, 4]), new(AllOf: [2, 5]),
        ],
        "cyclic-entry" =>
        [
            new(AnyOf: [2, 1]), new(DeclaresId: true, OneOf: [0, 3]),
            new(RequiresId: true), new(),
        ],
        "reference-siblings" =>
        [
            new(DeclaresId: true), new(ReferenceTarget: 0, RequiresId: true),
            new(ReferenceTarget: 1, AnyOf: [3, 4]), new(Discriminator: true), new(),
        ],
        "three-alternatives" =>
        [
            new(DeclaresId: true, RequiresId: true), new(DeclaresId: true), new(DeclaresId: true),
            new(OneOf: [0, 1, 2]), new(AllOf: [5, 6]), new(AnyOf: [3]), new(OneOf: [3]),
        ],
        _ when name.StartsWith("generated:", StringComparison.Ordinal) =>
            CreateGeneratedGraph(int.Parse(name["generated:".Length..], CultureInfo.InvariantCulture)),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown oracle graph."),
    };

    private static Node[] CreateGeneratedGraph(int seed)
    {
        var random = new Random(seed);
        var nodes = new Node[6];
        for (var i = 0; i < nodes.Length; i++)
        {
            var allOf = Enumerable.Range(0, random.Next(3)).Select(_ => random.Next(nodes.Length)).Distinct().ToArray();
            var first = random.Next(nodes.Length);
            var second = random.Next(nodes.Length - 1);
            var alternatives = new[] { first, second >= first ? second + 1 : second };
            nodes[i] = new Node(
                DeclaresId: i < 2,
                RequiresId: random.Next(4) == 0,
                Discriminator: random.Next(8) == 0,
                AllOf: allOf,
                AnyOf: i >= 3 && random.Next(2) == 0 ? alternatives : null,
                OneOf: i >= 3 && random.Next(2) == 0 ? alternatives.Reverse().ToArray() : null,
                ReferenceTarget: i == 2 && random.Next(2) == 0 ? random.Next(2) : null);
        }
        return nodes;
    }

    private sealed record Node(bool DeclaresId = false, bool RequiresId = false, bool Discriminator = false,
        int[]? AllOf = null, int[]? AnyOf = null, int[]? OneOf = null, int? ReferenceTarget = null);

    public void Dispose() => httpClient.Dispose();
}
