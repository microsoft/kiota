using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class PythonPathSegmenterTests
    {
        private readonly PythonPathSegmenter segmenter;
        public PythonPathSegmenterTests()
        {
            segmenter = new PythonPathSegmenter("D:\\source\\repos\\kiota-sample", "client");
        }

        [Fact]
        public void PythonPathSegmenterGeneratesCorrectFileName()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var classExample = rootNamespace.AddClass(new CodeClass
            {
                Name = "testClass"
            }).First();
            var fileName = segmenter.NormalizeFileName(classExample);
            Assert.Equal("test_class", fileName);// the file name should be snake case
        }

        [Fact]
        public void PythonPathSegmenterGeneratesNamespaceFolderName()
        {
            var namespaceName = "MicrosoftGraph";
            var normalizedNamespace = segmenter.NormalizeNamespaceSegment(namespaceName);
            Assert.Equal("microsoft_graph", normalizedNamespace);// the namespace name should be snake case
        }
    }
}
