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

  it "warns about a deprecated path when deprecation warnings are on" do
    client = Integration_test::Client::ApiClient.new(MicrosoftKiotaFaraday::FaradayRequestAdapter.new(MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new))
    deprecated_warnings = Warning[:deprecated]
    begin
      Warning[:deprecated] = true
      expect { client.api.something.v1.legacy.to_get_request_information }
        .to output(/integration_test_query_params\.rb:\d+: warning: Use v1 directly as of 2.0 on 2026-01-01 and will be removed 2027-01-01\n\z/).to_stderr
      expect { client.api.something.v1.to_get_request_information }.not_to output.to_stderr

      Warning[:deprecated] = false
      expect { client.api.something.v1.legacy.to_get_request_information }.not_to output.to_stderr
    ensure
      Warning[:deprecated] = deprecated_warnings
    end
  end
end
