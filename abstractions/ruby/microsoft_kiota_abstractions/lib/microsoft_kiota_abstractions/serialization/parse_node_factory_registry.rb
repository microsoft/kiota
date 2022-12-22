require_relative 'parse_node_factory'

module MicrosoftKiotaAbstractions
  class ParseNodeFactoryRegistry
    include ParseNodeFactory

    class << self
    attr_accessor :default_instance
    def default_instance; @default_instance ||= ParseNodeFactoryRegistry.new; end
    end

    def default_instance
    self.class.default_instance
    end

    def content_type_associated_factories
      @content_type_associated_factories ||= Hash.new
    end

    def get_parse_node(content_type, content)
      if !content_type
        raise Exception.new 'content type cannot be undefined or empty'
      end
      if !content
        raise Exception.new 'content cannot be undefined or empty'
      end
      vendor_specific_content_type = content_type.split(';').first
      factory = @content_type_associated_factories[vendor_specific_content_type]
      if factory
        return factory.get_parse_node(vendor_specific_content_type, content)
      end

      clean_content_type = vendor_specific_content_type.gsub(/[^\/]+\+/i, '')
      factory = @content_type_associated_factories[clean_content_type]
      if factory
        return factory.get_parse_node(clean_content_type, content)
      end
      raise Exception.new "Content type #{contentType} does not have a factory to be parsed"
    end
        
  end
end
