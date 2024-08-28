﻿using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters;

public class GoPathSegmenterTests
{
    private readonly GoPathSegmenter segmenter = new("D:\\source\\repos\\kiota-sample", "client");

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

    [Fact]
    public void GoPathSegmenterGeneratesDistinctFileNames()
    {
        var fileName1 = segmenter.NormalizeFileName(new CodeClass
        {
            Name = "codeScanningVariantAnalysisStatus"
        });
        var fileName2 = segmenter.NormalizeFileName(new CodeClass
        {
            Name = "codeScanningVariantAnalysis_status"
        });
        Assert.NotEqual(fileName1, fileName2);// the file name should be Different!
        Assert.Equal("code_scanning_variant_analysis_status", fileName1);// the file name should be camel cased!
        Assert.Equal("code_scanning_variant_analysis_escaped_status", fileName2);// the file name should be camel cased!
    }

    [Theory]
    [InlineData("adminWindows_escaped", "admin_windows_escaped")]//This technically won't be an expected escaped name in go as its refiner appends `Escaped`
    [InlineData("adminWindows_Escaped", "admin_windows_escaped")]//This technically won't be an expected escaped name in go as its refiner appends `Escaped`
    [InlineData("adminWindowsescaped", "admin_windowsescaped")]//This technically won't be an expected escaped name in go as its refiner appends `Escaped`
    [InlineData("adminWindowsEscaped", "admin_windows_escaped")]//This IS an expected escaped name in go as its refiner appends `Escaped`
    public void GoPathSegmenterGeneratesEscapedAlreadyEscapedSpecialClassName(string inputClassName, string expectedFileName)
    {
        var fileName = segmenter.NormalizeFileName(new CodeClass
        {
            Name = inputClassName
        });
        Assert.Equal(expectedFileName, fileName); // file name should be snake case and escaped
    }
}
