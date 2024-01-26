using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class CSharpPathSegmenterTests
    {
        private readonly CSharpPathSegmenter segmenter;
        public CSharpPathSegmenterTests()
        {
            segmenter = new CSharpPathSegmenter("D:\\source\\repos\\kiota-sample", "client");
        }

        [Fact]
        public void CSharpPathSegmenterGeneratesCorrectFileName()
        {
            var fileName = segmenter.NormalizeFileName(new CodeClass
            {
                Name = "testClass"
            });
            Assert.Equal("TestClass", fileName);// the file name should be PascalCase
        }

        [Fact]
        public void CSharpPathSegmenterDoesNotGenerateNamespaceFoldersThatAreTooLong()
        {
            var longNamespace = "ThisIsAVeryLongNamespaceNameThatShouldBeTruncatedAsItIsLongerThanTwoHundredAndFiftySixCharactersAccordingToWindows";
            longNamespace = longNamespace + longNamespace + longNamespace; //make it more than 256
            var normalizedNamespace = segmenter.NormalizeNamespaceSegment(longNamespace);
            Assert.NotEqual(longNamespace, normalizedNamespace);
            Assert.Equal(64, normalizedNamespace.Length);// shortened to sha256 length
        }

        [Fact]
        public void CSharpPathSegmenterDoesNotGeneratePathsThatAreTooLong()
        {
            var rootDir = "D:\\source\\repos\\kiota-sample\\Item\\ErpBOLaborSvc\\";
            var longNamespace = "ThisIsAVeryLongNamespaceNameThatIsNotLongerThanTwoHundredAndFiftySixCharactersAccordingToWindows";
            while (rootDir.Length < CSharpPathSegmenter.MaxFilePathLength - longNamespace.Length)
            {
                rootDir = $"{rootDir}\\{longNamespace}";
            }
            var longPathName = $"{rootDir}\\TimeWeeklyViewsWithCompanyWithEmployeeNumWithWeekBeginDateWithWeekEndDateWithQuickEntryCodeWithProjectIDWithPhaseIDWithTimeTypCdWithJobNumWithAssemblySeqWithOprSeqWithRoleCdWithIndirectCodeWithExpenseCodeWithResourceGrpIDWithResourceIDWithLaborTypePseudoWithShiftWithNewRowTypeWithTimeStatusRequestBuilder.cs";
            var normalizedPath = segmenter.NormalizePath(longPathName);
            Assert.NotEqual(longPathName, normalizedPath);
            Assert.True(normalizedPath.Length < longPathName.Length);//new path is shorter than the original
            Assert.True(normalizedPath.Length < CSharpPathSegmenter.MaxFilePathLength); // new path is shorter than the max path length
        }
    }
}
