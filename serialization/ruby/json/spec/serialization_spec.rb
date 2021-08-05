require_relative 'spec_helper'

RSpec.describe "Serialization" do

  it "can build jsonParseNode" do
    json_parse_node = MicrosoftKiotaSerialization::JsonParseNode.new(`{"value": [{"hasAttachments": false}] }`)
    expect(json_parse_node).not_to be nil
  end

end
