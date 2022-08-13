# frozen_string_literal: true
require 'uri'
require 'microsoft_kiota_abstractions'


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

  it "initializes a duration with ISO-formatted string or hash" do 
    time1 = MicrosoftKiotaAbstractions::ISODuration.new("P2Y1MT2H")
    time2 = MicrosoftKiotaAbstractions::ISODuration.new({ :years => 2, :months => 1, :hours => 2 } )
    expect(time1).to eq(time2)
  end

  it "fails on malformed string inputs" do 
    expect { MicrosoftKiotaAbstractions::ISODuration.new("P2Y1M3WT2H") }.to raise_error ISO8601::Errors::UnknownPattern
  end

  it "fails on malformed hash inputs" do
    expect { MicrosoftKiotaAbstractions::ISODuration.new({ :laughter => 2, :months => 1, :hours => 2 }) }.to raise_error('The key laughter is not recognized')
  end

  it "handles addition" do 
    time1 = MicrosoftKiotaAbstractions::ISODuration.new("P2Y1MT2H")
    time2 = MicrosoftKiotaAbstractions::ISODuration.new({ :years => 2, :months => 1, :hours => 2 } )
    time3 = time1 + time2
    expect(time3.string).to eq(MicrosoftKiotaAbstractions::ISODuration.new("P4Y2MT4H").string)
  end

  it "handles subtraction" do
    time1 = MicrosoftKiotaAbstractions::ISODuration.new("P4Y2MT2H")
    time2 = MicrosoftKiotaAbstractions::ISODuration.new({ :years => 1, :months => 1, :hours => 2 } )
    time3 = time1 - time2

    expect(time3.string).to eq(MicrosoftKiotaAbstractions::ISODuration.new("P3Y1M").string)
  end

  it "handles equality comparisons" do 
    time1 = MicrosoftKiotaAbstractions::ISODuration.new("P4Y2MT2H")
    time2 = MicrosoftKiotaAbstractions::ISODuration.new({ :years => 4, :months => 2, :hours => 2 } )
    expect(time1).to eq(time2)
  end
end
