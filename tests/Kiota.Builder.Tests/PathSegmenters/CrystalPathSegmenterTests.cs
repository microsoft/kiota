using System;
using System.IO;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters;

public sealed class CrystalPathSegmenterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string ClientNamespaceName = "TestNamespace";
    private readonly CrystalPathSegmenter pathSegmenter;

    public CrystalPathSegmenterTests()
    {
        pathSegmenter = new CrystalPathSegmenter(DefaultPath, ClientNamespaceName);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetsFileSuffix()
    {
        Assert.Equal(".cr", pathSegmenter.FileSuffix);
    }

    [Fact]
    public void NormalizesFileName()
    {
        var codeElement = new CodeClass { Name = "TestClass" };
        var result = pathSegmenter.NormalizeFileName(codeElement);
        Assert.Equal("test_class", result);
    }

    [Fact]
    public void NormalizesNamespaceSegment()
    {
        var result = pathSegmenter.NormalizeNamespaceSegment("TestNamespace");
        Assert.Equal("test_namespace", result);
    }

    [Fact]
    public void NormalizesPath()
    {
        var longPath = new string('a', 300);
        var result = pathSegmenter.NormalizePath(longPath);
        Assert.True(result.Length <= 230);
    }

    [Fact]
    public void GetsAdditionalSegment()
    {
        var codeNamespace = CodeNamespace.InitRootNamespace();
        codeNamespace.Name = "AnotherNamespace";
        var result = pathSegmenter.GetAdditionalSegment(codeNamespace, "file_name");
        Assert.Contains("file_name", result);
    }

    [Fact]
    public void GetsRelativeFileName()
    {
        var codeNamespace = CodeNamespace.InitRootNamespace();
        var codeClass = new CodeClass { Name = "TestClass" };
        codeNamespace.AddClass(codeClass);
        var result = pathSegmenter.GetRelativeFileName(codeNamespace, codeClass);
        Assert.Equal("test_class", result);
    }

    [Fact]
    public void ExceedsMaxPathLength()
    {
        var longPath = new string('a', 300);
        var result = pathSegmenter.ExceedsMaxPathLength(longPath);
        Assert.True(result);
    }
}


