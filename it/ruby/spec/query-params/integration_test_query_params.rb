# frozen_string_literal: true

RSpec.describe Integration_test do
  def uri_with(**query)
    client = Integration_test::Client::ApiClient.new(MicrosoftKiotaFaraday::FaradayRequestAdapter.new(MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new))
    config = MicrosoftKiotaAbstractions::RequestConfiguration.new
    config.query_parameters = Integration_test::Client::Api::Something::V1::V1RequestBuilder::V1RequestBuilderGetQueryParameters.new
    query.each { |name, value| config.query_parameters.public_send(:"#{name}=", value) }
    request_info = client.api.something.v1.to_get_request_information(config)
    request_info.path_parameters["baseurl"] = "http://test"
    request_info.uri.to_s
  end

  it "includes some query parameter" do
    expect(uri_with(start_date_time: "START")).to eq("http://test/api/something/v1?startDateTime=START")
  end

  it "includes some other query parameter" do
    expect(uri_with(end_date_time: "END")).to eq("http://test/api/something/v1?EndDateTime=END")
  end

  it "includes all the query parameters" do
    expect(uri_with(start_date_time: "START", end_date_time: "END")).to eq("http://test/api/something/v1?EndDateTime=END&startDateTime=START")
  end
end
