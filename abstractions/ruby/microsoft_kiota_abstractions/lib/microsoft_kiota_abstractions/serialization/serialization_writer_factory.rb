module MicrosoftKiotaAbstractions
    module SerializationWriterFactory
        def get_serialization_writer(content_type)
            raise NotImplementedError.new
        end
    end
end
