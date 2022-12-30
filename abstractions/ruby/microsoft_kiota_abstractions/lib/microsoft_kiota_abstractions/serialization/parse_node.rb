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

    def get_time_value()
      raise NotImplementedError.new
    end

    def get_date_time_value()
      raise NotImplementedError.new
    end

    def get_duration_value()
      raise NotImplementedError.new 
    end

    def get_collection_of_primitive_values()
      raise NotImplementedError.new
    end

    def get_collection_of_object_values(factory)
      raise NotImplementedError.new
    end

    def get_object_value(factory)
      raise NotImplementedError.new
    end

    def assign_field_values(item)
      raise NotImplementedError.new
    end

    def get_enum_value(type)
      raise NotImplementedError.new
    end

    def get_child_node(name)
      raise NotImplementedError.new
    end
    
  end
end
