require_relative 'serialization/parsable'
module MicrosoftKiotaAbstractions
    class ApiError < StandardError
        include MicrosoftKiotaAbstractions::Parsable

        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers()
            return Hash.new
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer)
            raise StandardError, 'writer cannot be null' if writer.nil?
        end

    end
end