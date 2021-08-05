require 'microsoft_kiota_abstractions'

module MicrosoftKiotaSerialization
    class JsonSerializationWriter
        include MicrosoftKiotaAbstractions::SerializationWriter
        @writer = Hash.new()
        def write_string_value(key, value)
            if !key && !value
                raise ArgumentError.new "no key or value included in write_string_value(key, value)"
            end
            if !key
                return value.to_s
            end
            @writer[key] = value
        end
        def write_boolean_value(key, value)
            if !key && !value
                raise ArgumentError.new "no key or value included in write_boolean_value(key, value)"
            end
            if !key 
                return value
            end
            @writer[key] = value
        end
        def write_number_value(key, value)
            if !key && !value
                raise ArgumentError.new "no key or value included in write_number_value(key, value)"
            end
            if !key
                return value
            end
            @writer[key] = value
        end
        def write_guid_value(key, value)
            if !key && !value
                raise ArgumentError.new "no key or value included in write_guid_value(key, value)"
            end
            if !key
                return value.to_s
            end
            @writer[key] = value.to_s
        end
        # TODO confirm date format needed to serialize data
        def write_date_value(key, value)
            if !key && !value
                raise ArgumentError.new "no key or value included in write_date_value(key, value)"
            end
            if !key
                return value.strftime("%Y-%m-%dT%H:%M:%S%Z")
            end
            @writer[key] = value.strftime("%Y-%m-%dT%H:%M:%S%Z")
        end
        def write_collection_of_primitive_values(key, values)
            if values
                if !key
                    return values.map do |v|
                        self.write_any_value(nil, v)
                    end
                end
                @writer[key] = values.map do |v|
                    self.write_any_value(nil, v)
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
                    self.write_object_value(nil, v)
                end
            end
        end
        def write_object_value(key, value)
            if value
                if !key
                    return value.serialize(self);
                end
                @writer[key] = value.serialize(self)
            end
        end
        def write_enum_value(key, values)
            if values.length > 0
                raw_values = values.select{|v| v != nil}.map{|v| v.to_s}
                if raw_values.length > 0
                    this.write_string_value(key, raw_values.join(", "))
                end
            end
        end
        def get_serialized_content()
            return @current_node.to_json
        end
        def write_additional_data(value)
            if !value
                return
            end
            value.each do |x, y|
                this.write_any_value(x,y)
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
                elsif value.instance_of? Time
                    return self.write_date_value(key, value)
                elsif value.instance_of? Array
                    return self.write_collection_of_primitive_values(key, value)
                elsif value.is_a? Object
                    return value.to_s
                else
                    raise ArgumentError.new "encountered unknown value type during serialization #{value.to_s}"
                end
            else
                if key
                    @writer[key] = nil
                else
                    raise ArgumentError.new "no key included when writing json property"
                end
            end
                
        end
    end
end
