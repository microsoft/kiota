# frozen_string_literal: true

RSpec.describe MicrosoftKiotaAbstractions do
  it "has a version number" do
    expect(MicrosoftKiotaAbstractions::VERSION).not_to be nil
  end

  it "tests library method" do
    request_obj = MicrosoftKiotaAbstractions::RequestInfo.new
    expect(!request_obj).to eq(false)
  end
end
