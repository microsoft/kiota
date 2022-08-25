module MicrosoftKiotaAbstractions
  module ParseNodeFactory
    def self.get_parse_node(content_type, content)
      raise NotImplementedError.new
    end
  end
end
