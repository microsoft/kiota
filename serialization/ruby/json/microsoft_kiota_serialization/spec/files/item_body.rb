require 'microsoft_kiota_abstractions'

module Files
    class ItemBody
        include MicrosoftKiotaAbstractions::AdditionalDataHolder, MicrosoftKiotaAbstractions::Parsable
        ## 
        # Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        @additional_data
        ## 
        # The content of the item.
        @content
        ## 
        # The contentType property
        @content_type
        ## 
        ## Gets the additionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        ## @return a i_dictionary
        ## 
        def additional_data
            return @additional_data
        end
        ## 
        ## Sets the additionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
        ## @param value Value to set for the AdditionalData property.
        ## @return a void
        ## 
        def additional_data=(value)
            @additional_data = value
        end
        ## 
        ## Instantiates a new itemBody and sets the default values.
        ## @return a void
        ## 
        def initialize()
            @additional_data = Hash.new
        end
        ## 
        ## Gets the content property value. The content of the item.
        ## @return a string
        ## 
        def content
            return @content
        end
        ## 
        ## Sets the content property value. The content of the item.
        ## @param value Value to set for the content property.
        ## @return a void
        ## 
        def content=(value)
            @content = value
        end
        ## 
        ## Gets the contentType property value. The contentType property
        ## @return a body_type
        ## 
        def content_type
            return @content_type
        end
        ## 
        ## Sets the contentType property value. The contentType property
        ## @param value Value to set for the contentType property.
        ## @return a void
        ## 
        def content_type=(value)
            @content_type = value
        end
        ## 
        ## Creates a new instance of the appropriate class based on discriminator value
        ## @param parseNode The parse node to use to read the discriminator value and create the object
        ## @return a item_body
        ## 
        def self.create_from_discriminator_value(parse_node)
            raise StandardError, 'parse_node cannot be null' if parse_node.nil?
            return ItemBody.new
        end
        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers()
            return {
                "content" => lambda {|n| @content = n.get_string_value() },
                "contentType" => lambda {|n| @content_type = n.get_enum_value(Files::BodyType) },
            }
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer)
            raise StandardError, 'writer cannot be null' if writer.nil?
            writer.write_string_value("content", @content)
            writer.write_enum_value("contentType", @content_type)
            writer.write_additional_data(@additional_data)
        end
    end
end
