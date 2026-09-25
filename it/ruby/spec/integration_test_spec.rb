# frozen_string_literal: true

RSpec.describe Integration_test do
  it "has a version number" do
    expect(Integration_test::VERSION).not_to be nil
  end

  it "does something useful" do
    context = MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new("9A2BF795-AB23-46CF-BA7D-08F48912CEE0", "E4650AC0-9E59-4997-8215-31D3A42B9A8B", "foo")
    auth_provider = MicrosoftKiotaAuthenticationOAuth::OAuthAuthenticationProvider.new(context, nil, nil)
    api = Integration_test::Client::ApiClient.new(MicrosoftKiotaFaraday::FaradayRequestAdapter.new(auth_provider))
    expect(api).to_not be nil
  end

  # models are autoloaded, so a bad registration only fails when the constant is first used
  it "resolves every constant the client registers" do
    visit = lambda do |mod, seen|
      next if seen.key?(mod)

      seen[mod] = true
      mod.constants(false).each do |name|
        value = mod.const_get(name, false)
        visit.call(value, seen) if value.instance_of?(Module)
      end
    end
    expect { visit.call(Integration_test::Client, {}) }.not_to raise_error
  end
end
