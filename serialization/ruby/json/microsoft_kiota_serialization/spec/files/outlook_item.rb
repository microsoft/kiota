require 'microsoft_kiota_abstractions'
require_relative './entity'

module Files
    class OutlookItem < Entity
        include MicrosoftKiotaAbstractions::Parsable
        ## 
        # The categories associated with the item
        @categories
        ## 
        # Identifies the version of the item. Every time the item is changed, changeKey changes as well. This allows Exchange to apply changes to the correct version of the object. Read-only.
        @change_key
        ## 
        # The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        @created_date_time
        ## 
        # The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        @last_modified_date_time
        ## 
        ## Gets the categories property value. The categories associated with the item
        ## @return a string
        ## 
        def  categories
            return @categories
        end
        ## 
        ## Gets the changeKey property value. Identifies the version of the item. Every time the item is changed, changeKey changes as well. This allows Exchange to apply changes to the correct version of the object. Read-only.
        ## @return a string
        ## 
        def  change_key
            return @change_key
        end
        ## 
        ## Gets the createdDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        ## @return a date_time_offset
        ## 
        def  created_date_time
            return @created_date_time
        end
        ## 
        ## Gets the lastModifiedDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        ## @return a date_time_offset
        ## 
        def  last_modified_date_time
            return @last_modified_date_time
        end
        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers() 
            return {
                "categories" => lambda {|o, n| o.categories = n.get_collection_of_primitive_values(String) },
                "changeKey" => lambda {|o, n| o.change_key = n.get_string_value() },
                "createdDateTime" => lambda {|o, n| o.created_date_time = n.get_Date_value() },
                "lastModifiedDateTime" => lambda {|o, n| o.last_modified_date_time = n.get_Date_value() },
            }
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer) 
            super.serialize(writer)
            writer.write_collection_of_primitive_values("categories", @categories)
            writer.write_string_value("changeKey", @change_key)
            writer.write_object_value("createdDateTime", @created_date_time)
            writer.write_object_value("lastModifiedDateTime", @last_modified_date_time)
        end
        ## 
        ## Sets the categories property value. The categories associated with the item
        ## @param value Value to set for the categories property.
        ## @return a void
        ## 
        def  categories=(categories)
            @categories = categories
        end
        ## 
        ## Sets the changeKey property value. Identifies the version of the item. Every time the item is changed, changeKey changes as well. This allows Exchange to apply changes to the correct version of the object. Read-only.
        ## @param value Value to set for the changeKey property.
        ## @return a void
        ## 
        def  change_key=(changeKey)
            @change_key = changeKey
        end
        ## 
        ## Sets the createdDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        ## @param value Value to set for the createdDateTime property.
        ## @return a void
        ## 
        def  created_date_time=(createdDateTime)
            @created_date_time = createdDateTime
        end
        ## 
        ## Sets the lastModifiedDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
        ## @param value Value to set for the lastModifiedDateTime property.
        ## @return a void
        ## 
        def  last_modified_date_time=(lastModifiedDateTime)
            @last_modified_date_time = lastModifiedDateTime
        end
    end
end
