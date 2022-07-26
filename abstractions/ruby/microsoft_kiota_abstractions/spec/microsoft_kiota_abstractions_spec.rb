require 'uri'
# frozen_string_literal: true

RSpec.describe MicrosoftKiotaAbstractions do
  it "has a version number" do
    expect(MicrosoftKiotaAbstractions::VERSION).not_to be nil
  end

  it "tests library method" do
    request_obj = MicrosoftKiotaAbstractions::RequestInformation.new
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
    expect { token_provider.authenticate_request(MicrosoftKiotaAbstractions::RequestInformation.new()) }.to raise_error(NotImplementedError)
  end

  it "returns the raw URI when set via setter" do
    request_obj = MicrosoftKiotaAbstractions::RequestInformation.new
    request_obj.path_parameters["term"] = "search"
    request_obj.query_parameters["q1"] = "option1"
    request_obj.uri = "https://www.bing.com"
    expect(request_obj.uri).to eq(URI("https://www.bing.com"))
    expect(request_obj.path_parameters).to eq({})
    expect(request_obj.query_parameters).to eq({})
  end

  it "returns the raw URI when set via raw url parmeter" do
    request_obj = MicrosoftKiotaAbstractions::RequestInformation.new
    request_obj.path_parameters["request-raw-url"] = "https://www.bing.com"
    expect(request_obj.path_parameters).to eq({ "request-raw-url" => "https://www.bing.com"})
    request_obj.path_parameters["term"] = "search"
    request_obj.query_parameters["q1"] = "option1"
    expect(request_obj.uri).to eq(URI("https://www.bing.com"))
    expect(request_obj.path_parameters).to eq({})
    expect(request_obj.query_parameters).to eq({})
  end

  it "returns a templated url" do
    request_obj = MicrosoftKiotaAbstractions::RequestInformation.new
    request_obj.url_template = "https://www.bing.com/{term}{?q1,q2}"
    request_obj.path_parameters["term"] = "search"
    request_obj.query_parameters["q1"] = "option1"
    expect(request_obj.uri).to eq(URI("https://www.bing.com/search?q1=option1"))
  end
end
