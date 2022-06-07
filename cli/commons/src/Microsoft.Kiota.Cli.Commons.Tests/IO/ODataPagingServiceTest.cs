using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Cli.Commons.IO;
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
            var requestInfo = new RequestInformation();
            var pagingData = new PageLinkData(requestInfo, null);

            var nextLink = await pagingService.GetNextPageLinkAsync(pagingData);

            Assert.Null(nextLink);
        }

        [Fact]
        public async Task Return_Next_Link_From_Response()
        {
            var pagingService = new ODataPagingService();
            var bytes = Encoding.UTF8.GetBytes("{\"nextLink\": \"https://testlink\"}");
            using var ms = new MemoryStream(bytes);
            var requestInfo = new RequestInformation();
            requestInfo.Headers.Add("Accept", "application/json");
            var pagingData = new PageLinkData(requestInfo, ms);
            pagingData.ResponseHeaders.Add("ContentType", new string[] { "application/json" });

            var nextLink = await pagingService.GetNextPageLinkAsync(pagingData);

            Assert.Equal(new Uri("https://testlink"), nextLink);
        }

        [Fact]
        public async Task Return_Null_On_Next_Link_Missing()
        {
            var pagingService = new ODataPagingService();
            var bytes = Encoding.UTF8.GetBytes("{}");
            using var ms = new MemoryStream(bytes);
            var requestInfo = new RequestInformation();
            var pagingData = new PageLinkData(requestInfo, ms);

            var nextLink = await pagingService.GetNextPageLinkAsync(pagingData);

            Assert.Null(nextLink);
        }
    }

    public class MergeJsonStreamsFunction_Should
    {
        [Fact]
        public async Task Return_Null_on_Null_Streams()
        {
            var pagingService = new ODataPagingService();

            var response = await pagingService.MergeJsonStreamsAsync(null, null);

            Assert.Null(response);
        }

        [Fact]
        public async Task Return_Left_on_Null_Right_Stream()
        {
            var pagingService = new ODataPagingService();

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": [20]}"));
            var response = await pagingService.MergeJsonStreamsAsync(ms, null);

            Assert.Equal(ms, response);
        }

        [Fact]
        public async Task Return_Right_on_Null_Left_Stream()
        {
            var pagingService = new ODataPagingService();

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": [20]}"));
            var response = await pagingService.MergeJsonStreamsAsync(null, ms);

            Assert.Equal(ms, response);
        }

        [Fact]
        public async Task Return_Left_On_Null_Data_In_Right_Stream()
        {
            var pagingService = new ODataPagingService();

            using var leftMs = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": [20]}"));
            using var rightMs = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": null}"));
            var response = await pagingService.MergeJsonStreamsAsync(leftMs, rightMs);
            using var reader = new StreamReader(response ?? Stream.Null);
            var result = await reader.ReadToEndAsync();

            Assert.Equal("{\"value\": [20]}", result);
        }

        [Fact]
        public async Task Return_Right_On_Null_Data_In_Left_Stream()
        {
            var pagingService = new ODataPagingService();

            using var leftMs = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": null}"));
            using var rightMs = new MemoryStream(Encoding.UTF8.GetBytes("{\"value\": [20]}"));
            var response = await pagingService.MergeJsonStreamsAsync(leftMs, rightMs);
            using var reader = new StreamReader(response ?? Stream.Null);
            var result = await reader.ReadToEndAsync();

            Assert.Equal("{\"value\": [20]}", result);
        }

        [Theory]
        [InlineData(null, "[20,21,24]", "[30]", "[20,21,24,30]")]
        [InlineData("", "[20,21,24]", "[30]", "[20,21,24,30]")]
        [InlineData("value", "{\"value\": [20]}", "{\"value\": [30]}", "{\"value\":[20,30]}")]
        [InlineData("value", "{\"value\": [{\"a\": 1}, {\"a\": 2}]}", "{\"value\": [{\"b\": 4}]}", "{\"value\":[{\"a\":1},{\"a\":2},{\"b\":4}]}")]
        public async Task Return_Merged_Stream(string itemName, string left, string right, string expected)
        {
            var pagingService = new ODataPagingService();

            using var leftMs = new MemoryStream(Encoding.UTF8.GetBytes(left));
            using var rightMs = new MemoryStream(Encoding.UTF8.GetBytes(right));
            var response = await pagingService.MergeJsonStreamsAsync(leftMs, rightMs, itemName);
            using var reader = new StreamReader(response ?? Stream.Null);
            var result = await reader.ReadToEndAsync();

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("{\"value\":[20],\"nextLink\":\"test1\"}", "{\"value\":[30],\"nextLink\":\"test2\"}", "{\"value\":[20,30],\"nextLink\":\"test2\"}")]
        [InlineData("{\"value\":[{\"a\": 1}],\"nextLink\":\"test2\"}", "{\"value\":[{\"b\":4}],\"nextLink\":\"test2\"}", "{\"value\":[{\"a\":1},{\"b\":4}],\"nextLink\":\"test2\"}")]
        public async Task Return_With_Next_Link_From_Right_Stream(string left, string right, string expected)
        {
            var pagingService = new ODataPagingService();

            using var leftMs = new MemoryStream(Encoding.UTF8.GetBytes(left));
            using var rightMs = new MemoryStream(Encoding.UTF8.GetBytes(right));
            var response = await pagingService.MergeJsonStreamsAsync(leftMs, rightMs);
            using var reader = new StreamReader(response ?? Stream.Null);
            var result = await reader.ReadToEndAsync();

            Assert.Equal(expected, result);
        }
    }
}
