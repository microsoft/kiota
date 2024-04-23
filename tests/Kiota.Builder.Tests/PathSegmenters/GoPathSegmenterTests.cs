using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters;

public class GoPathSegmenterTests
{
    private readonly GoPathSegmenter segmenter;
    public GoPathSegmenterTests()
    {
        segmenter = new GoPathSegmenter("D:\\source\\repos\\kiota-sample", "client");
    }

    [Fact]
    public void GoPathSegmenterGeneratesCorrectFileName()
    {
        var fileName = segmenter.NormalizeFileName(new CodeClass
        {
            Name = "testClass"
        });
        Assert.Equal("test_class", fileName); // file name should be snake case
    }

    [Fact]
    public void GoPathSegmenterGeneratesEscapedSpecialClassName()
    {
        var fileName = segmenter.NormalizeFileName(new CodeClass
        {
            Name = "adminWindows"
        });
        Assert.Equal("admin_windows_escaped", fileName); // file name should be snake case and escaped
    }

    [Fact]
    public void GoPathSegmenterGeneratesNamespaceFolderName()
    {
        var namespaceName = "Microsoft.Graph";
        var normalizedNamespace = segmenter.NormalizeNamespaceSegment(namespaceName);
        Assert.Equal("microsoft.graph", normalizedNamespace);// the file name should be lower case
    }
}
