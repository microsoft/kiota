using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed class OpenApiPropertyFilterDependencyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProtectsTransitiveDependenciesOnlyWhenTheTriggerSurvivesAsync(bool retainTrigger)
    {
        using var httpClient = new HttpClient();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {
              "openapi": "3.1.0", "info": {"title": "Dependencies", "version": "1"}, "paths": {},
              "components": {"schemas": {
                "Root": {"allOf": [
                  {"$ref": "#/components/schemas/Triggers"},
                  {"$ref": "#/components/schemas/Targets", "dependentRequired": {
                    "trigger": ["middle"], "middle": ["last"]
                  }}
                ]},
                "Triggers": {"properties": {"keep": {}, "trigger": {}}},
                "Targets": {"properties": {"keep": {}, "middle": {}, "last": {}, "unrelated": {}}}
              }}
            }
            """));
        var document = Assert.IsType<OpenApiDocument>(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration(), httpClient).CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken));
        var properties = document.Components!.Schemas!["Targets"].Properties!;
        var rules = Assert.IsType<OpenApiSchemaReference>(document.Components.Schemas["Root"].AllOf![1]).Reference.DependentRequired!;
        var filter = new OpenApiPropertyFilter(NullLogger.Instance);
        var config = new GenerationConfiguration { IncludeProperties = ["Triggers#keep", "Targets#keep"] };
        if (retainTrigger)
        {
            config.IncludeProperties.Add("Triggers#trigger");
            config.ExcludeProperties.Add("Targets#last");
            var error = Assert.Throws<InvalidOperationException>(() => filter.Apply(document, config, TestContext.Current.CancellationToken));
            Assert.Contains("dependentRequired", error.Message, StringComparison.Ordinal);
            Assert.Contains("unrelated", properties.Keys);
            config.ExcludeProperties.Clear();
        }

        filter.Apply(document, config, TestContext.Current.CancellationToken);

        string[] expected = retainTrigger ? ["keep", "last", "middle"] : ["keep"];
        Assert.Equal(expected, properties.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("middle", rules["trigger"]);
        Assert.Contains("last", rules["middle"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesNotCombineDependenciesAcrossDifferentSharedUnionBranches(bool splitTriggerAndTarget)
    {
        var trigger = SchemaWithProperties("trigger");
        var target = SchemaWithProperties("keep", "dependent");
        var rule = new OpenApiSchema
        {
            DependentRequired = new Dictionary<string, HashSet<string>> { ["trigger"] = ["dependent"] },
        };
        var choice = splitTriggerAndTarget
            ? new OpenApiSchema { OneOf = [trigger, target] }
            : new OpenApiSchema { OneOf = [rule, new OpenApiSchema { AllOf = [trigger, target] }] };
        var root = new OpenApiSchema
        {
            AllOf = [new OpenApiSchema { AllOf = [choice] }, new OpenApiSchema { AllOf = [choice] }],
        };
        if (splitTriggerAndTarget)
            root.AllOf!.Add(rule);
        var document = DocumentWithRoot(root);

        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document,
            new GenerationConfiguration { ExcludeProperties = ["Root#dependent"] }, TestContext.Current.CancellationToken);

        Assert.Contains("trigger", trigger.Properties!.Keys);
        Assert.DoesNotContain("dependent", target.Properties!.Keys);
    }

    [Fact]
    public void CanceledAnalysisDoesNotMutateDeclarations()
    {
        var root = SchemaWithProperties("keep", "remove");
        using var source = new CancellationTokenSource();
        source.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(DocumentWithRoot(root), new GenerationConfiguration { IncludeProperties = ["Root#keep"] }, source.Token));

        Assert.Equal(source.Token, error.CancellationToken);
        Assert.Contains("remove", root.Properties!.Keys);
    }

    [Fact]
    public void WorkLimitStopsWideSymbolicAnalysisBeforeMutatingDeclarations()
    {
        var root = SchemaWithProperties("keep", "remove");
        root.Required = new HashSet<string>(StringComparer.Ordinal) { "remove" };
        root.AnyOf = Enumerable.Repeat<IOpenApiSchema>(new OpenApiSchema(), 150_000).ToList();

        var error = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(DocumentWithRoot(root), new GenerationConfiguration { IncludeProperties = ["Root#keep"] }, TestContext.Current.CancellationToken));

        Assert.Contains("work limit", error.Message, StringComparison.Ordinal);
        Assert.Contains("remove", root.Properties!.Keys);
    }

    [Fact]
    public void DepthLimitStopsDeepSymbolicConditionsBeforeMutatingDeclarations()
    {
        var root = SchemaWithProperties("keep", "remove");
        root.Required = new HashSet<string>(StringComparer.Ordinal) { "remove" };
        var common = new OpenApiSchema();
        root.AllOf = Enumerable.Range(0, 300).Select(_ => (IOpenApiSchema)new OpenApiSchema
        {
            OneOf = [common, new OpenApiSchema()],
        }).ToList();

        var error = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(DocumentWithRoot(root), new GenerationConfiguration { IncludeProperties = ["Root#keep"] }, TestContext.Current.CancellationToken));

        Assert.Contains("condition depth limit", error.Message, StringComparison.Ordinal);
        Assert.Contains("remove", root.Properties!.Keys);
    }

    private static OpenApiSchema SchemaWithProperties(params string[] properties) =>
        new()
        {
            Properties = properties.ToDictionary(static name => name, static _ => (IOpenApiSchema)new OpenApiSchema(), StringComparer.Ordinal)
        };

    private static OpenApiDocument DocumentWithRoot(OpenApiSchema root) =>
        new()
        {
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents { Schemas = new Dictionary<string, IOpenApiSchema> { ["Root"] = root } },
        };
}
