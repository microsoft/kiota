using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevLab.JmesPath;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Cli.Commons.IO;
using Moq;
using Spectre.Console;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class ODataPagingServiceTest
{
    public class GetNextPageLinkAsyncFunction_Should
    {
        [Fact]
        public async Task Return_Null_on_No_Next_Link()
        {
            var pagingService = new ODataPagingService();
            var stream = Stream.Null;
            var requestInfo = new RequestInformation();
            requestInfo.Headers["Accept"] = "application/json";
            var pagingData = new PageLinkData(requestInfo, stream);

            var nextLink = await pagingService.GetNextPageLinkAsync(pagingData);

            Assert.Null(nextLink);
        }

        [Fact]
        public async Task Return_Next_Link_From_Response()
        {
            var pagingService = new ODataPagingService();
            var bytes = Encoding.UTF8.GetBytes("{\"nextLink\": \"https://testlink\"}");
            var ms = new MemoryStream(bytes);
            var requestInfo = new RequestInformation();
            requestInfo.Headers["Accept"] = "application/json";
            var pagingData = new PageLinkData(requestInfo, ms);

            var nextLink = await pagingService.GetNextPageLinkAsync(pagingData);

            Assert.Equal(new Uri("https://testlink"), nextLink);
        }
    }
}
