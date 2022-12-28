require 'microsoft_kiota_abstractions'
require_relative 'parse_node_factory_mock'
RSpec.describe MicrosoftKiotaAbstractions do
    values = []
    values << "application/json"
    values << "application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8"
    values << "application/vnd.github.mercy-preview+json"
    values << "application/vnd.github.mercy-preview+json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8"
    serialized_value = "[1]"
    MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance.content_type_associated_factories["application/json"] = ParseNodeFactoryMock.new
    it "gets the parse node" do
      values.each do |value|
        expect(MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance.get_parse_node(value, serialized_value)).not_to be nil
      end
    end
end