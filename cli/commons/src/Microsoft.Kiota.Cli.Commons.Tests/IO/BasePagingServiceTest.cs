using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Cli.Commons.IO;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class BasePagingServiceTest
{
    public class GetPagedDataAsyncFunction_Should
    {
        private readonly Mock<IPagingResponseHandler> responseHandlerMock;

        private readonly Mock<BasePagingService> pagingServiceMock;

        private readonly Stream stream1;

        private readonly Stream streamMerged;

        public GetPagedDataAsyncFunction_Should()
        {
            pagingServiceMock = new(MockBehavior.Strict);
            responseHandlerMock = new();
            stream1 = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            streamMerged = new MemoryStream(Encoding.UTF8.GetBytes("hellohello"));
            responseHandlerMock.Setup(h => h.GetResponseStreamAsync(default)).ReturnsAsync(stream1);
            responseHandlerMock.Setup(h => h.GetStatusCode()).Returns(200);

            pagingServiceMock.Setup(ps => ps.CreateResponseHandler()).Returns(responseHandlerMock.Object);
            pagingServiceMock.Setup(ps => ps.GetPagedDataAsync(It.IsAny<Func<RequestInformation, IResponseHandler, CancellationToken, Task>>(), It.IsAny<PageLinkData>(), It.IsAny<bool>(), default)).CallBase();
            pagingServiceMock.Setup(ps => ps.OnBeforeGetPagedData(It.IsAny<PageLinkData>(), It.IsAny<bool>())).CallBase();
            pagingServiceMock.Setup(ps => ps.MergePageAsync(It.IsAny<Stream?>(), It.IsAny<PageLinkData>(), default)).ReturnsAsync(streamMerged);
            pagingServiceMock.Setup(ps => ps.GetNextPageLinkAsync(It.IsAny<PageLinkData>(), default)).Returns(Task.FromResult<Uri?>(null));
        }

        [Fact]
        public async Task Call_OnBeforeGetPagedData()
        {
            var requestInfo = new RequestInformation();
            var pagingData = new PageLinkData(requestInfo, null);
            pagingServiceMock.Setup(ps => ps.OnBeforeGetPagedData(pagingData, false)).Returns(false);

            var pagingService = pagingServiceMock.Object;

            var response = await pagingService.GetPagedDataAsync((info, handler, token) => Task.CompletedTask, pagingData);

            pagingServiceMock.Verify(ps => ps.OnBeforeGetPagedData(pagingData, false), Times.Once);
        }

        [Fact]
        public async Task Handle_Unpaged_Requests()
        {
            var requestInfo = new RequestInformation();
            var pagingData = new PageLinkData(requestInfo, null);
            var pagingService = pagingServiceMock.Object;

            var response = await pagingService.GetPagedDataAsync((info, handler, token) => Task.CompletedTask, pagingData);

            Assert.Equal(200, response?.StatusCode);
            Assert.Equal(streamMerged, response?.Response);
            pagingServiceMock.Verify(ps => ps.GetNextPageLinkAsync(It.IsAny<PageLinkData>(), default), Times.Never);
        }

        [Fact]
        public async Task Handle_Paged_Requests()
        {
            var requestInfo = new RequestInformation();
            var pagingData = new PageLinkData(requestInfo, null);
            var pagingService = pagingServiceMock.Object;
            var count = 0;
            pagingServiceMock.Setup(ps => ps.GetNextPageLinkAsync(It.IsAny<PageLinkData>(), default)).ReturnsAsync(() =>
            {
                if (count > 0)
                {
                    return null;
                }

                count++;
                return new Uri("https://testlink");
            });

            var response = await pagingService.GetPagedDataAsync((info, handler, token) => Task.CompletedTask, pagingData, true);

            Assert.Equal(200, response?.StatusCode);
            Assert.Equal(streamMerged, response?.Response);
            pagingServiceMock.Verify(ps => ps.GetNextPageLinkAsync(It.IsAny<PageLinkData>(), default), Times.Exactly(2));
        }
    }
}
