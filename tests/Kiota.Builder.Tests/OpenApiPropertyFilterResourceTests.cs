using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed class OpenApiPropertyFilterResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task MeasureIsolatedSampleAsync()
    {
        var scenario = Environment.GetEnvironmentVariable("KIOTA_PROPERTY_FILTER_RESOURCE_SCENARIO") ?? string.Empty;
        Assert.SkipWhen(scenario.Length == 0, "Run it/measure-property-filtering.ps1 for isolated resource measurements.");
        var size = int.Parse(Environment.GetEnvironmentVariable("KIOTA_PROPERTY_FILTER_RESOURCE_SIZE") ?? "24",
            System.Globalization.CultureInfo.InvariantCulture);
        var mode = Environment.GetEnvironmentVariable("KIOTA_PROPERTY_FILTER_RESOURCE_MODE");
        Assert.Contains(mode, new[] { "baseline", "filtered" });
        Assert.True(size > 0);
        var includes = JsonSerializer.Deserialize<string[]>(Environment.GetEnvironmentVariable("KIOTA_PROPERTY_FILTER_RESOURCE_INCLUDES") ?? "[]")!;
        if (mode == "filtered")
            Assert.NotEmpty(includes);
        await using Stream stream = scenario == "file"
            ? File.OpenRead(Environment.GetEnvironmentVariable("KIOTA_PROPERTY_FILTER_RESOURCE_DESCRIPTION")!)
            : new MemoryStream(CreateSyntheticDescription(scenario, size));
        using var httpClient = new HttpClient();
        var parse = Stopwatch.StartNew();
        var document = await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration(), httpClient)
            .CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        parse.Stop();
        Assert.NotNull(document);
        var before = CountDeclarations(document);
        var configuration = new GenerationConfiguration
        {
            IncludeProperties = mode == "filtered" ? includes.ToHashSet(StringComparer.Ordinal) : [],
        };
        GC.Collect();
        GC.WaitForPendingFinalizers();
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var workingSetBefore = process.WorkingSet64;
        var allocatedBefore = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration);
        watch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        process.Refresh();
        output.WriteLine("PROPERTY_FILTER_METRICS " + JsonSerializer.Serialize(new
        {
            Scenario = scenario,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Size = scenario == "file" ? (int?)null : size,
            Mode = mode,
            IncludeProperties = configuration.IncludeProperties.Order(StringComparer.Ordinal).ToArray(),
            ParseMilliseconds = parse.Elapsed.TotalMilliseconds,
            FilterMilliseconds = watch.Elapsed.TotalMilliseconds,
            FilterAllocatedBytes = allocated,
            ProcessPeakWorkingSetBytes = process.PeakWorkingSet64,
            WorkingSetBeforeFilterBytes = workingSetBefore,
            ComponentCount = document.Components?.Schemas?.Count ?? 0,
            DeclarationsBefore = before,
            DeclarationsAfter = CountDeclarations(document),
        }));
    }

    private static int CountDeclarations(OpenApiDocument document)
    {
        var counter = new DeclarationCounter();
        new OpenApiWalker(counter).Walk(document);
        return counter.Count;
    }

    private sealed class DeclarationCounter : OpenApiVisitorBase
    {
        private readonly HashSet<IOpenApiSchema> visited = new(ReferenceEqualityComparer.Instance);
        internal int Count
        {
            get; private set;
        }
        public override void Visit(IOpenApiSchema schema)
        {
            if (schema is OpenApiSchema && visited.Add(schema))
                Count += schema.Properties?.Count ?? 0;
        }
    }

    private static byte[] CreateSyntheticDescription(string scenario, int size)
    {
        static Dictionary<string, object> Reference(string name) => new() { ["$ref"] = $"#/components/schemas/{name}" };
        static Dictionary<string, object> Model() => new()
        {
            ["type"] = "object",
            ["properties"] = new
            {
                id = new
                {
                    type = "string"
                },
                keep = new
                {
                    type = "string"
                },
                remove = new
                {
                    type = "string"
                }
            },
        };
        var schemas = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Target"] = Model(),
            ["Constraint"] = new { required = new[] { "id" } },
        };
        switch (scenario)
        {
            case "flat":
                for (var i = 0; i < size; i++)
                {
                    var model = Model();
                    model["required"] = new[] { "id" };
                    schemas[$"Model{i}"] = model;
                }
                break;
            case "diamond":
                var root = Model();
                root["anyOf"] = new[] { Reference("Layer0A"), Reference("Layer0B") };
                schemas["Root"] = root;
                for (var i = 0; i < size; i++)
                    foreach (var suffix in new[] { "A", "B" })
                        schemas[$"Layer{i}{suffix}"] = new
                        {
                            anyOf = new[]
                            {
                                Reference(i == size - 1 ? "Constraint" : $"Layer{i + 1}A"),
                                Reference(i == size - 1 ? "Constraint" : $"Layer{i + 1}B"),
                            },
                        };
                break;
            case "cycle":
                for (var i = 0; i < size; i++)
                {
                    var model = Model();
                    model["anyOf"] = new[] { Reference($"Node{(i + 1) % size}"), Reference("Constraint") };
                    schemas[$"Node{i}"] = model;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown resource scenario.");
        }
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            openapi = "3.1.0",
            info = new
            {
                title = $"Property filter {scenario}",
                version = "1.0.0"
            },
            paths = new
            {
            },
            components = new
            {
                schemas
            },
        });
    }
}
