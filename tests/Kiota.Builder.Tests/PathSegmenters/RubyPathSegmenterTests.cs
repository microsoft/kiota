using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class RubyPathSegmenterTests
    {
        private readonly RubyPathSegmenter segmenter;
        public RubyPathSegmenterTests()
        {
            segmenter = new RubyPathSegmenter("/tmp/kiota-sample", "client");
        }

        [Fact]
        public void RubyPathSegmenterGeneratesCorrectFileName()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var classExample = rootNamespace.AddClass(new CodeClass
            {
                Name = "testClass"
            }).First();
            Assert.Equal("test_class", segmenter.NormalizeFileName(classExample));
        }

        [Fact]
        public void DisambiguatesEnumsThatSnakeCaseToTheSameFileName()
        {
            // the github description declares both a top level status enum and an inline one on the
            // parent schema; snake casing collapses them onto one path, so one silently overwrote
            // the other and the models barrel required the same file twice
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            var withSeparator = ns.AddEnum(new CodeEnum { Name = "codeScanningVariantAnalysis_status" }).First();
            var withoutSeparator = ns.AddEnum(new CodeEnum { Name = "codeScanningVariantAnalysisStatus" }).First();

            var first = segmenter.NormalizeFileName(withSeparator);
            var second = segmenter.NormalizeFileName(withoutSeparator);
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void DisambiguatesAClassAndAnEnumSharingAFileName()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            var model = ns.AddClass(new CodeClass { Name = "SomeModel", Kind = CodeClassKind.Model }).First();
            var enumeration = ns.AddEnum(new CodeEnum { Name = "some_model" }).First();

            Assert.NotEqual(segmenter.NormalizeFileName(model), segmenter.NormalizeFileName(enumeration));
        }

        [Fact]
        public void KeepsTheFileNameStableWhenThereIsNoCollision()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            var only = ns.AddClass(new CodeClass { Name = "someModel", Kind = CodeClassKind.Model }).First();
            ns.AddClass(new CodeClass { Name = "otherModel", Kind = CodeClassKind.Model });

            Assert.Equal("some_model", segmenter.NormalizeFileName(only));
        }

        [Fact]
        public void ReturnsTheSameNameForRepeatedCalls()
        {
            // the writer resolves the require path and the file writer resolves the output path
            // through separate calls, so they have to agree
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            ns.AddEnum(new CodeEnum { Name = "codeScanningVariantAnalysis_status" });
            var second = ns.AddEnum(new CodeEnum { Name = "codeScanningVariantAnalysisStatus" }).First();

            Assert.Equal(segmenter.NormalizeFileName(second), segmenter.NormalizeFileName(second));
        }
    }
}
