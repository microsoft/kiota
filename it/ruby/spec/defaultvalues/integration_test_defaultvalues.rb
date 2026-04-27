# frozen_string_literal: true

RSpec.describe Integration_test do
  it "test default value initialization of model class" do
    auth_provider = MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new()
    request_adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(auth_provider)
    request_adapter.set_base_url('http://127.0.0.1:1080')
    client = Integration_test::Client::ApiClient.new(request_adapter)
    
    service_response = client.api().v1().weather_forecast().get().resume
    # Here, a single object is returned instead of a list. So we just check for null
    expect(service_response).to_not be nil
    
    # pp service_response
    # pp service_response.enum_value().class
    
    # Now the real test: create a model class and verify that all properties have the default values.
    model = Integration_test::Client::Models::WeatherForecast.new()
    
    expect(model.bool_value()).to be true

    expect(model.date_only_value).to_not be nil
    expect(model.date_only_value.to_s()).to eq("1900-01-01")
    
    expect(model.date_value).to_not be nil
    expect(model.date_value.to_s()).to eq("1900-01-01T00:00:00+00:00")
    
    expect(model.decimal_value()).to be 25.5
    expect(model.double_value()).to be 25.5
    expect(model.enum_value()).to be Integration_test::Client::Models::WeatherForecastEnumValue[:One]
    expect(model.float_value()).to be 25.5

    expect(model.guid_value).to_not be nil
    expect(model.guid_value().to_s()).to eq("00000000-0000-0000-0000-000000000000")
    
    expect(model.long_value()).to be 255
    expect(model.summary()).to eq("Test")
    expect(model.temperature_c()).to be 15
    
    expect(model.time_value()).to_not be nil
    expect(model.time_value().strftime("%H:%M:%S")).to eq("00:00:00")
  end
end
