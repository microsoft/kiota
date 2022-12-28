require 'microsoft_kiota_abstractions'
require_relative 'serialization_writer_factory_mock'
RSpec.describe MicrosoftKiotaAbstractions do
    values = []
    values << "application/json"
    values << "application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8"
    values << "application/vnd.github.mercy-preview+json"
    values << "application/vnd.github.mercy-preview+json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8"
    MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance.content_type_associated_factories["application/json"] = SerializationWriterFactoryMock.new
    it "gets the serialization writer" do
      values.each do |value|
        expect(MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance.get_serialization_writer(value)).not_to be nil
      end
    end
end