module MicrosoftKiotaAbstractions
    module Parsable
        def self.additional_data 
            @additional_data ||= Hash.new
        end

        def get_field_deserializers
            raise NotImplementedError.new
        end

        def serialize(writer)
            raise NotImplementedError.new
        end
    end
end
