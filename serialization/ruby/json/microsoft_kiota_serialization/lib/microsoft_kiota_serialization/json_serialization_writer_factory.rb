require 'microsoft_kiota_abstractions'

module MicrosoftKiotaSerialization
  class JsonSerializationWriterFactory
    include MicrosoftKiotaAbstractions::SerializationWriterFactory

    def get_valid_content_type
      'application/json'
    end

    def get_serialization_writer(content_type)
      if !content_type
        raise StandardError, 'content type cannot be undefined or empty'
      elsif get_valid_content_type != content_type
        raise StandardError, `expected a #{get_valid_content_type} content type`
      end

      JsonSerializationWriter.new
    end
  end
end
