# frozen_string_literal: true

RSpec.describe Integration_test do
  it "raises the typed error declared by the description" do
    auth_provider = MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new()
    request_adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(auth_provider)
    request_adapter.set_base_url('http://127.0.0.1:1080')
    client = Integration_test::Client::ApiClient.new(request_adapter)
    expect(client).to_not be nil

    expect { client.api().v1().topics().get().resume }
      .to raise_error(Integration_test::Client::Models::Error) { |error|
        expect(error.id).to eq("my-sample-id")
        expect(error.code).to be 123
      }
  end
end
