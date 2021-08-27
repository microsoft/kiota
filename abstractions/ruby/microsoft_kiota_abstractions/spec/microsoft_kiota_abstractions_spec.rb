# frozen_string_literal: true

RSpec.describe MicrosoftKiotaAbstractions do
  it "has a version number" do
    expect(MicrosoftKiotaAbstractions::VERSION).not_to be nil
  end

  it "tests library method" do
    request_obj = MicrosoftKiotaAbstractions::RequestInfo.new
    expect(!request_obj).to eq(false)
  end

  it "creates a anonymous token provider" do
    token_provider = MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new()
    expect(token_provider).not_to be nil
  end

  it "creates a bearer token provider" do
    token_provider = MicrosoftKiotaAbstractions::BaseBearerTokenAuthenticationProvider.new()
    expect(token_provider).not_to be nil
  end

  it "throws if the token method is not implemented" do
    token_provider = MicrosoftKiotaAbstractions::BaseBearerTokenAuthenticationProvider.new()
    expect { token_provider.authenticate_request(MicrosoftKiotaAbstractions::RequestInfo.new()) }.to raise_error(NotImplementedError)
  end
end
