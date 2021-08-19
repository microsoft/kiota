require 'microsoft_kiota_abstractions'
require 'time'
require 'json'
require "uuidtools"

module MicrosoftKiotaSerialization
  class JsonSerializationWriter
    include MicrosoftKiotaAbstractions::SerializationWriter
    @writer

    def initialize()
      @writer = Hash.new()
    end

    def writer
      @writer
    end

    def write_string_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_string_value(key, value)"
      end
      if !key
        return value.to_s
      end
      if !value
        @writer[key] = nil
      else
        @writer[key] = value
      end
    end

    def write_boolean_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_boolean_value(key, value)"
      end
      if !key 
        return value
      end
      @writer[key] = value
    end

    def write_number_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_number_value(key, value)"
      end
      if !key
        return value
      end
      @writer[key] = value
    end

    def write_float_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_float_value(key, value)"
      end
      if !key
        return value
      end
      @writer[key] = value
    end

    def write_guid_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_guid_value(key, value)"
      end
      if !key
        return value.to_s
      end
      if !value
        @writer[key] = nil
      else
        @writer[key] = value.to_s
      end
    end

    def write_date_value(key, value)
      if !key && !value
        raise StandardError, "no key or value included in write_date_value(key, value)"
      end
      if !key
        return value.strftime("%Y-%m-%dT%H:%M:%S%Z")
      end
      if !value
        @writer[key] = nil
      else
        @writer[key] = value.strftime("%Y-%m-%dT%H:%M:%S%Z")
      end
    end

    def write_collection_of_primitive_values(key, values)
      if values
        if !key
          return values.map do |v|
            self.write_any_value(nil, v)
          end
        end
        @writer[key] = values.map do |v|
          self.write_any_value(key, v)
        end
      end
    end

    def write_collection_of_object_values(key, values)
      if values
        if !key
          return values.map do |v|
            self.write_object_value(nil, v)
          end
        end
        @writer[key] = values.map do |v|
          self.write_object_value(nil, v).writer
        end
      end
    end

    def write_object_value(key, value)
      if value
        if !key
          temp = JsonSerializationWriter.new()
          value.serialize(temp)
          return temp
        end
        begin
          temp = JsonSerializationWriter.new()
          value.serialize(temp)
          @writer[key] = temp.writer
        rescue StandardError => e
          raise e.class, "no key or value included in write_boolean_value(key, value)" 
        end
      end
    end

    def write_enum_value(key, values)
      self.write_string_value(key, values.to_s)
    end

    def get_serialized_content()
      return @writer.to_json #TODO encode to byte array to stay content type agnostic
    end

    def write_additional_data(value)
      if !value
        return
      end
      value.each do |x, y|
        self.write_any_value(x,y)
      end
    end

    def write_any_value(key, value)
      if value
        if !!value == value
          return value
        elsif value.instance_of? String
          return self.write_string_value(key, value)
        elsif value.instance_of? Integer
          return self.write_number_value(key, value)
        elsif value.instance_of? Float
          return self.write_float_value(key, value)
        elsif value.instance_of? Time
          return self.write_date_value(key, value)
        elsif value.instance_of? Array
          return self.write_collection_of_primitive_values(key, value)
        elsif value.is_a? Object
          return value.to_s
        else
          raise StandardError, "encountered unknown value type during serialization #{value.to_s}"
        end
      else
        if key
          @writer[key] = nil
        else
          raise StandardError, "no key included when writing json property"
        end
      end
    end
  end
end
