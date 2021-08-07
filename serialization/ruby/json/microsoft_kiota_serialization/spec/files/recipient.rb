require 'microsoft_kiota_abstractions'

module Files
    class Recipient
        include MicrosoftKiotaAbstractions::Parsable
        ## 
        # Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        @additional_data
        @email_address
        ## 
        ## Gets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        ## @return a i_dictionary
        ## 
        def  additional_data
            return @additional_data
        end
        ## 
        ## Gets the emailAddress property value. 
        ## @return a email_address
        ## 
        def  email_address
            return @email_address
        end
        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers() 
            return {
                "emailAddress" => lambda {|o, n| o.email_address = n.get_object_value(Files::EmailAddress) },
            }
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer) 
            writer.write_object_value("emailAddress", @email_address)
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
        ## Sets the emailAddress property value. 
        ## @param value Value to set for the emailAddress property.
        ## @return a void
        ## 
        def  email_address=(emailAddress)
            @email_address = emailAddress
        end
    end
end
