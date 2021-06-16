# frozen_string_literal: true
require 'uri'

RSpec.describe MicrosoftKiotaAbstractions do
  it "has a version number" do
    expect(MicrosoftKiotaAbstractions::VERSION).not_to be nil
  end

  it "tests library method" do
    request_obj = MicrosoftKiotaAbstractions::RequestInfo.new
    expect(!request_obj).to eq(false)
    request_obj.uri = URI("https://www.microsoft.com/")
    expect(!request_obj.uri).to eq(false)
  end
end
