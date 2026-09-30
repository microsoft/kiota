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
  it "eager loads every constant the client registers" do
    Integration_test::Client.eager_load!
    unloaded = []
    pending = [Integration_test::Client]
    until pending.empty?
      mod = pending.pop
      mod.constants(false).each do |name|
        next unloaded << "#{mod}::#{name}" if mod.autoload?(name, false)

        value = mod.const_get(name, false)
        pending << value if value.instance_of?(Module)
      end
    end

    expect(unloaded).to be_empty
  end

  it "resolves every constant the generated code names" do
    names = Dir[File.expand_path("../lib/integration_test/client/**/*.rb", __dir__)].flat_map do |file|
      File.read(file).scan(/Integration_test::Client(?:::[A-Z]\w*)+/)
    end
    unresolved = names.uniq.reject do |name|
      Object.const_get(name)
    rescue NameError
      false
    end

    expect(unresolved).to be_empty
  end
end
