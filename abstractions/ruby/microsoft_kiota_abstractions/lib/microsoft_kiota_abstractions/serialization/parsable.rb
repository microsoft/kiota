module MicrosoftKiotaAbstractions
  module Parsable
    def get_field_deserializers
      raise NotImplementedError.new
    end

    def serialize(writer)
      raise NotImplementedError.new
    end
  end
end
