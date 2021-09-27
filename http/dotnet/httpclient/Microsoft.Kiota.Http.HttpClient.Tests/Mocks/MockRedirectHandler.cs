using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Mocks
{
    public class MockRedirectHandler : HttpMessageHandler
    {
        private HttpResponseMessage Response1
        {
            get; set;
        }
        private HttpResponseMessage Response2
        {
            get; set;
        }

        private bool _response1Sent = false;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(!_response1Sent)
            {
                _response1Sent = true;
                Response1.RequestMessage = request;
                return await Task.FromResult(Response1);
            }
            else
            {
                _response1Sent = false;
                Response2.RequestMessage = request;
                return await Task.FromResult(Response2);
            }
        }

        public void SetHttpResponse(HttpResponseMessage response1, HttpResponseMessage response2 = null)
        {
            this._response1Sent = false;
            this.Response1 = response1;
            this.Response2 = response2;
        }

    }
}
