using App.Client;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions.Authentication;
using App.Client.Models;

namespace Kiota.IT.MockServerTests;
public class KiotaMockServerTests
{
    /// <summary>
    /// Tests that default values of a model class are applied when creating a new instance.
    /// </summary>
    [Fact]
    public async Task DefaultValuesInModelClassTest()
    {
        var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider());
        requestAdapter.BaseUrl = "http://localhost:1080";
        var client = new ApiClient(requestAdapter);

        //Call a sample endpoint - not really needed here.
        List<WeatherForecast>? modelList = await client.Api.V1.WeatherForecast.GetAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(modelList);
        Assert.Single(modelList);

        //Now the real test: create a model class and verify that all properties have the default values.
        WeatherForecast model = new WeatherForecast();
        Assert.True(model.BoolValue);

        Assert.NotNull(model.DateOnlyValue);
        Assert.Equal("1900-01-01", model.DateOnlyValue.Value.ToString());

        Assert.NotNull(model.DateValue);
        Assert.Equal("1900-01-01 00:00", model.DateValue.Value.ToString("yyyy-MM-dd HH:mm"));

        Assert.Equal(25.5, model.DecimalValue);
        Assert.Equal(25.5, model.DoubleValue);
        Assert.Equal(WeatherForecast_enumValue.One, model.EnumValue);
        Assert.Equal(25.5f, model.FloatValue);

        Assert.NotNull(model.GuidValue);
        Assert.Equal("00000000-0000-0000-0000-000000000000", model.GuidValue.Value.ToString());

        Assert.Equal(255, model.LongValue);
        Assert.Equal("Test", model.Summary);
        Assert.Equal(15, model.TemperatureC);

        Assert.NotNull(model.TimeValue);
        Assert.Equal("00:00:00", model.TimeValue.Value.ToString());

    }
}
