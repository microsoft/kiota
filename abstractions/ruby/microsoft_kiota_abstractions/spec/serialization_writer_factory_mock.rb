require 'microsoft_kiota_abstractions'
class SerializationWriterFactoryMock
    include MicrosoftKiotaAbstractions::SerializationWriterFactory

    def get_valid_content_type
      'application/json'
    end
    def get_serialization_writer(clean_content)
        return {}
    end
end