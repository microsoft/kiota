module MicrosoftKiotaAbstractions
  module SerializationWriter

    def write_string_value(key, value)
      raise NotImplementedError.new
    end

    def write_boolean_value(key, value)
      raise NotImplementedError.new
    end

    def write_number_value(key, value)
      raise NotImplementedError.new
    end

    def write_float_value(key, value)
      raise NotImplementedError.new
    end

    def get_date_value(key, value)
      raise NotImplementedError.new
    end

    def write_guid_value(key, value)
      raise NotImplementedError.new
    end

    def write_date_value(key, value)
      raise NotImplementedError.new
    end

    def write_collection_of_primitive_values(key, value)
      raise NotImplementedError.new
    end

    def write_collection_of_object_values(key, value)
      raise NotImplementedError.new
    end

    def write_enum_value(key, value)
      raise NotImplementedError.new
    end

    def get_serialized_content()
      raise NotImplementedError.new
    end

    def write_additional_data(type)
      raise NotImplementedError.new
    end

    def write_any_value(key, value)
      raise NotImplementedError.new
    end
    
  end
end
