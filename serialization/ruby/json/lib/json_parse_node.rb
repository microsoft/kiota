require 'microsoft_kiota_abstractions'
require 'json'

module MicrosoftKiotaSerialization
    class JsonParseNode
        include MicrosoftKiotaAbstractions::Parsable
        @current_node
        def initialize(node)
            @current_node = node
        end
        def get_string_value()
            return @current_node.to_s
        end
        def get_boolean_value()
            return @current_node
        end
        def get_number_value()
            return @current_node.to_i
        end
        def get_guid_value()
            return UUIDTools::UUID.parse(@current_node)
        end
        def get_date_value()
            return Time.parse(@current_node)
        end
        def get_collection_of_primitive_values()
            return @current_node.map do |x|
                current_parse_node = JsonParseNode.new(x)
                begin
                    date = current_parse_node.get_date_value()
                    date
                rescue ArgumentError
                    begin
                        guid = current_parse_node.get_guid_value()
                        guid
                    rescue ArgumentError
                        val = current_parse_node.get_scalar_value()
                        val
                    end
                end
            end
        end
        def get_collection_of_object_values(type)
            return @current_node.map do |x|
                current_parse_node = JsonParseNode.new(x)
                val = current_parse_node.get_object_value(type)
                val
            end
        end
        def get_object_value(type)
            begin
                item = type.new()
                self.assign_field_values(item)
                item
            rescue => exception
                raise Exception.new 'Error during deserialization'
            end
        end
        def assign_field_values(item)
            fields = item.get_field_deserializers()
            @current_node.each do |k, v| 
                deserializer = fields[k]
                if deserializer
                    deserializer.(item, JsonParseNode.new(v))
                else
                    if item.additional_data
                        item.additional_data[k] = v
                    else
                        item.additional_data = Hash.new(k => v)
                    end
                end
                    
            end
        end
        def get_enum_value(type)
            return @current_node.to_sym
        end
    end
end
