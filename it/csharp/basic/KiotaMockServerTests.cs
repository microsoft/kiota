using App.Client;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions.Authentication;
using App.Client.Models;

namespace Kiota.IT.MockServerTests;
public class KiotaMockServerTests
{
    [Fact]
    public async Task BasicEndpointTest()
    {
        var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider());
        requestAdapter.BaseUrl = "http://localhost:1080";
        var client = new ApiClient(requestAdapter);

        Error error = await Assert.ThrowsAsync<Error>(() => client.Api.V1.Topics.GetAsync());

        Assert.Equal("my-sample-id", error.Id);
        Assert.Equal(123, error.Code);
    }
}
