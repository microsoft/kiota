require_relative 'serialization_writer_factory'

module MicrosoftKiotaAbstractions
  class SerializationWriterFactoryRegistry
    include SerializationWriterFactory

    class << self
      attr_accessor :default_instance
      def default_instance; @default_instance ||= SerializationWriterFactoryRegistry.new; end
    end

    def default_instance
      self.class.default_instance
    end

    def content_type_associated_factories
      @content_type_associated_factories ||= Hash.new
    end

    def get_serialization_writer(content_type)
      if !content_type
        raise Exception.new 'content type cannot be undefined or empty'
      end
      factory = @content_type_associated_factories[content_type]
      if factory
        return factory.get_serialization_writer(content_type)
      else
        raise Exception.new "Content type #{contentType} does not have a factory to be serialized"
      end
    end
    
  end
end
