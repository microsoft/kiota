require 'microsoft_kiota_abstractions'

module Files
    class EmailAddress
        include MicrosoftKiotaAbstractions::Parsable
        ## 
        # Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        @additional_data
        ## 
        # The email address of an entity instance.
        @address
        ## 
        # The display name of an entity instance.
        @name
        ## 
        ## Gets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        ## @return a i_dictionary
        ## 
        def  additional_data
            return @additional_data
        end
        ## 
        ## Gets the address property value. The email address of an entity instance.
        ## @return a string
        ## 
        def  address
            return @address
        end
        ## 
        ## Gets the name property value. The display name of an entity instance.
        ## @return a string
        ## 
        def  name
            return @name
        end
        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers() 
            return {
                "address" => lambda {|o, n| o.address = n.get_string_value() },
                "name" => lambda {|o, n| o.name = n.get_string_value() },
            }
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer) 
            writer.write_string_value("address", @address)
            writer.write_string_value("name", @name)
            writer.write_additional_data(self.additional_data)
        end
        ## 
        ## Sets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        ## @param value Value to set for the AdditionalData property.
        ## @return a void
        ## 
        def  additional_data=(additionalData)
            @additional_data = additionalData
        end
        ## 
        ## Sets the address property value. The email address of an entity instance.
        ## @param value Value to set for the address property.
        ## @return a void
        ## 
        def  address=(address)
            @address = address
        end
        ## 
        ## Sets the name property value. The display name of an entity instance.
        ## @param value Value to set for the name property.
        ## @return a void
        ## 
        def  name=(name)
            @name = name
        end
    end
end
