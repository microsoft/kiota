using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;

using Xunit;

namespace Kiota.Builder.Tests.CodeRenderers;

public class RubyCodeRendererTests
{
    private readonly RubyCodeRenderer renderer = new(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = "graph" });
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

    [Fact]
    public void IsTheRendererForRuby() =>
        Assert.IsType<RubyCodeRenderer>(CodeRenderer.GetCodeRender(new GenerationConfiguration { Language = GenerationLanguage.Ruby }));

    [Fact]
    public void SkipsTheBarrelOfANamespaceWithOnlyRequestBuilders()
    {
        var ns = root.AddNamespace("graph.users");
        ns.AddClass(new CodeClass { Name = "usersRequestBuilder", Kind = CodeClassKind.RequestBuilder });
        Assert.False(renderer.ShouldRenderNamespaceFile(ns));
    }

    [Fact]
    public void WritesTheBarrelOfANamespaceWithAModel()
    {
        var ns = root.AddNamespace("graph.models");
        ns.AddClass(new CodeClass { Name = "user", Kind = CodeClassKind.Model });
        Assert.True(renderer.ShouldRenderNamespaceFile(ns));
    }

    [Fact]
    public void WritesTheBarrelOfANamespaceWithAnEnumThatHasOptions()
    {
        var ns = root.AddNamespace("graph.models");
        ns.AddEnum(new CodeEnum { Name = "color" }).First().AddOption(new CodeEnumOption { Name = "red" });
        Assert.True(renderer.ShouldRenderNamespaceFile(ns));
    }

    [Fact]
    public void SkipsTheBarrelWhenAClassTakesItsFile()
    {
        var ns = root.AddNamespace("graph.models");
        ns.AddClass(new CodeClass { Name = "models", Kind = CodeClassKind.Model });
        ns.AddClass(new CodeClass { Name = "user", Kind = CodeClassKind.Model });
        Assert.False(renderer.ShouldRenderNamespaceFile(ns));
    }
}
