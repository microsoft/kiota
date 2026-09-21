using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task PreservesPathsWithCollidingSanitizedNamesAsync(bool reverse, bool reserveSuffix, bool modelsNamespace)
    {
        var paths = reserveSuffix ? new[] { "/v1.1/tests", "/v11/tests", "/v11Escaped/tests" } : new[] { "/v1.1/tests", "/v11/tests" };
        if (modelsNamespace)
            paths = ["/models/tests", "/modelsRequests/tests", "/modelsRequestsEscaped/tests"];
        if (reverse)
            paths = paths.Reverse().ToArray();
        var operations = string.Join(",", paths.Select(path => $"\"{path}\":{{\"get\":{{\"responses\":{{\"204\":{{\"description\":\"OK\"}}}}}}}}"));
        var description = """
            {"openapi":"3.0.3","info":{"title":"Colliding paths","version":"1"},"paths":{PATHS}}
            """.Replace("PATHS", operations);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var client = model.FindChildByName<CodeClass>("ApiClient");
        Assert.NotNull(client);
        var navigation = client.Properties.Where(x => x.Kind == CodePropertyKind.RequestBuilder).ToArray();
        Assert.Equal(paths.Length, navigation.Length);
        Assert.Contains(navigation, x => x.Name == (modelsNamespace ? "models" : "v11"));
        var targets = navigation.Select(x => Assert.IsType<CodeClass>(Assert.IsType<CodeType>(x.Type).TypeDefinition)).ToArray();
        Assert.Equal(paths.Length, targets.Distinct().Count());
        var templates = targets.SelectMany(x => x.Properties.Where(p => p.Kind == CodePropertyKind.RequestBuilder))
            .Select(x => Assert.IsType<CodeClass>(Assert.IsType<CodeType>(x.Type).TypeDefinition))
            .Select(x => x.GetPropertyOfKind(CodePropertyKind.UrlTemplate).DefaultValue).ToArray();
        foreach (var path in paths)
            Assert.Contains($"\"{{+baseurl}}{path}\"", templates);
    }
}
