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

  it 'initializes empty allowed hosts properly' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new([])
    expect(ahv.allowed_hosts).to eq({})
  end

  it 'initializes non-empty/cased allowed hosts properly' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['microsoft.com', 'Graph.microsoft.com', 'DOD-graph.microsoft.us'])
    valid_hosts = ahv.allowed_hosts
    expect(valid_hosts).to eq({'microsoft.com' => true, 'graph.microsoft.com' => true, 'dod-graph.microsoft.us' => true})
  end

  it 'tests the setter for allowed hosts on allowed hosts validator' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['microsoft.com', 'Graph.microsoft.com', 'DOD-graph.microsoft.us'])
    ahv.allowed_hosts = ['MICROSOFT.com', 'GRAPH.microsoft.COM', 'DOD-graph.microsoft.us', 'graph.microsoft.de']
    expect(ahv.allowed_hosts).to eq({'microsoft.com' => true, 'graph.microsoft.com' => true, 'dod-graph.microsoft.us' => true, 'graph.microsoft.de' => true})
  end

  it 'tests url_host_valid? method on malformed and valid urls' do
    ahv = MicrosoftKiotaAbstractions::AllowedHostsValidator.new(['www.google.com', 'example.com', 'Graph.microsoft.com',
                                                                 'DOD-graph.microsoft.us', "cool/groovy/art"])
    url1 = ahv.url_host_valid?("https://www.google.com")
    url2 = ahv.url_host_valid?("htts://google.com")
    url3 = ahv.url_host_valid?("cool/groovy/art")
    url4 = ahv.url_host_valid?("https://example.com")
    url5 = ahv.url_host_valid?('https%3A%2F%2Fwww.example.com')
    url6 = ahv.url_host_valid?('%3A%2F%2F')
    expect(url1).to eq(true)
    expect(url2).to eq(false)
    expect(url3).to eq(false)
    expect(url4).to eq(true)
    expect(url5).to eq(false)
    expect(url5).to eq(false)
    expect(url6).to eq(false)
  end

  it 'tests the default instance for ParseNodeFactoryRegistry is set' do
    expect(MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance).to_not be_nil
  end

  it 'tests the default instance for SerializationWriterFactoryRegistry is set' do
    expect(MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance).to_not be_nil
  end
end
