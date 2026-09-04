using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class DartPathSegmenterTests
    {
        private readonly DartPathSegmenter segmenter;
        public DartPathSegmenterTests()
        {
            segmenter = new DartPathSegmenter("/tmp/kiota-sample", "client");
        }

        [Fact]
        public void DartPathSegmenterGeneratesCorrectFileName()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var classExample = rootNamespace.AddClass(new CodeClass
            {
                Name = "testClass"
            }).First();
            Assert.Equal("test_class", segmenter.NormalizeFileName(classExample));
        }

        [Fact]
        public void DisambiguatesClassesThatSnakeCaseToTheSameFileName()
        {
            // an inline property type keeps its raw Parent_property name when the pascal cased
            // form is already taken by a component schema; snake casing collapses both onto one
            // path, so one silently overwrote the other and the imports referenced a type the
            // surviving file does not declare
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            var componentSchema = ns.AddClass(new CodeClass { Name = "ProcessError", Kind = CodeClassKind.Model }).First();
            var inlineType = ns.AddClass(new CodeClass { Name = "Process_error", Kind = CodeClassKind.Model }).First();

            var first = segmenter.NormalizeFileName(componentSchema);
            var second = segmenter.NormalizeFileName(inlineType);
            Assert.NotEqual(first, second);
            Assert.Equal("process_error", first);
            Assert.Equal("process_error_2", second);
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
            // the class declaration writer resolves the import path and the file writer resolves
            // the output path through separate calls, so they have to agree
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var ns = rootNamespace.AddNamespace("client.models");
            ns.AddClass(new CodeClass { Name = "ProcessError", Kind = CodeClassKind.Model });
            var second = ns.AddClass(new CodeClass { Name = "Process_error", Kind = CodeClassKind.Model }).First();

            Assert.Equal(segmenter.NormalizeFileName(second), segmenter.NormalizeFileName(second));
        }
    }
}
