require_relative 'serialization_writer_factory'

module MicrosoftKiotaAbstractions
    class SerializationWriterFactoryRegistry
        include SerializationWriterFactory

        def content_type_associated_factories
            @content_type_associated_factories ||= Hash.new
        end

        def get_serialization_writer(content_type)
            if !content_type
                raise Exception.new "content type cannot be undefined or empty"
            end
            factory = @content_type_associated_factories[content_type]
            if factory
                return factory.get_serialization_writer(content_type)
            else
                raise Exception.new "Content type #{contentType} does not have a factory to be serialized"
            end
        end
    end
end
