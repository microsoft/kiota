require 'microsoft_kiota_abstractions'

module MicrosoftKiotaSerialization
    class JsonSerializationWriterFactory
        include MicrosoftKiotaAbstractions::SerializationWriterFactory

        def get_valid_content_type()
            return "application/json"
        end

        def get_serialization_writer(content_type)
            if !content_type
				raise Exception.new 'content type cannot be undefined or empty'
            elsif self.get_valid_content_type() != content_type
                raise Exception.new `expected a #{self.get_valid_content_type()} content type`
            end
            return JsonSerializationWriter.new()
        end
    end
end
