module MicrosoftKiotaAbstractions
  module ParseNode

    def get_string_value()
      raise NotImplementedError.new
    end

    def get_boolean_value()
      raise NotImplementedError.new
    end

    def get_number_value()
      raise NotImplementedError.new
    end

    def get_guid_value()
      raise NotImplementedError.new
    end

    def get_date_value()
      raise NotImplementedError.new
    end

    def get_collection_of_primitive_values()
      raise NotImplementedError.new
    end

    def get_collection_of_object_values(type)
      raise NotImplementedError.new
    end

    def get_object_value(type)
      raise NotImplementedError.new
    end

    def assign_field_values(item)
      raise NotImplementedError.new
    end

    def get_enum_value(type)
      raise NotImplementedError.new
    end
    
  end
end
