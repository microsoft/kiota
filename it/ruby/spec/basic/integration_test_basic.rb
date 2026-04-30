# frozen_string_literal: true

RSpec.describe Integration_test do
  it "does something useful" do
    auth_provider = MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new()
    request_adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(auth_provider)
    request_adapter.set_base_url('http://127.0.0.1:1080')
    client = Integration_test::Client::ApiClient.new(request_adapter)
    expect(client).to_not be nil
    
    # Error: uninitialized constant Integration_test::Client::Api::V1::Topics::TopicsRequestBuilder::Binary
    # response = client.api().v1().topics().get().resume
    # pp response
  end
end
