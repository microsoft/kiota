require 'microsoft_kiota_abstractions'
require 'json'
require 'uuidtools'

module MicrosoftKiotaSerialization
  class JsonParseNode
    include MicrosoftKiotaAbstractions::ParseNode
    def initialize(node)
      @current_node = node
    end

    def get_string_value
      @current_node.to_s
    end

    def get_boolean_value
      @current_node
    end

    def get_number_value
      @current_node.to_i
    end

    def get_float_value
      @current_node.to_f
    end

    def get_guid_value
      UUIDTools::UUID.parse(@current_node)
    end

    def get_date_value
      Time.parse(@current_node)
    end

    def get_collection_of_primitive_values(_type)
      @current_node.map do |x|
        current_parse_node = JsonParseNode.new(x)
        begin
          date = current_parse_node.get_date_value
          date
        rescue ArgumentError
          begin
            guid = current_parse_node.get_guid_value
            guid
          rescue ArgumentError
            val = current_parse_node.get_string_value
            val
          end
        end
      end
    end

    def get_collection_of_object_values(type)
      @current_node.map do |x|
        current_parse_node = JsonParseNode.new(x)
        val = current_parse_node.get_object_value(type)
        val
      end
    end

    def get_object_value(type)
      item = type.new
      assign_field_values(item)
      item
    rescue StandardError
      raise StandardError, 'Error during deserialization'
    end

    def assign_field_values(item)
      fields = item.get_field_deserializers
      @current_node.each do |k, v|
        deserializer = fields[k]
        if deserializer
          deserializer.call(item, JsonParseNode.new(v))
        elsif item.additional_data
          item.additional_data[k] = v
        else
          item.additional_data = Hash.new(k => v)
        end
      end
    end

    def get_enum_values(_type)
      raw_values = get_string_value
      raw_values.split(',').map(&:strip)
    end

    def get_enum_value(type)
      items = get_enum_values(type).map(&:to_sym)
      items[0] if items.length.positive?
    end
  end
end
