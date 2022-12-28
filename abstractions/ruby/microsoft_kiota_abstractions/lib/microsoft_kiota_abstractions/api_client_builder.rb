require_relative "serialization/parse_node_factory"
require_relative "serialization/parse_node_factory_registry"
require_relative "serialization/serialization_writer_factory"
require_relative "serialization/serialization_writer_factory_registry"

module MicrosoftKiotaAbstractions
  class ApiClientBuilder
    def self.register_default_serializer(factory_class)
      factory = factory_class.new()
      MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance.content_type_associated_factories[factory.get_valid_content_type()] = factory
    end

    def self.register_default_deserializer(factory_class)
      factory = factory_class.new()
      MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance.content_type_associated_factories[factory.get_valid_content_type()] = factory
    end
  end
end
  
