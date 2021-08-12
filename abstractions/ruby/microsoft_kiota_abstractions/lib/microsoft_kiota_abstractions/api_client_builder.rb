require_relative "serialization/parse_node_factory"
require_relative "serialization/parse_node_factory_registry"
require_relative "serialization/serialization_writer_factory"
require_relative "serialization/serialization_writer_factory_registry"

module MicrosoftKiotaAbstractions
    module ApiClientBuilder
  
      def get_authorization_token(request_url)
        raise NotImplementedError.new
      end

      #TODO: Implement default de/serializer registration in the api_client generation issue #478
      def register_default_serializer(factory_class)
        begin
            factory = factory_class.new()
            MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.new().content_type_associated_factories[factory.get_valid_content_type(), factory]
        rescue => exception
            raise exception
        end
      end

      def register_default_deserializer(factory_class)
        begin
            factory = factory_class.new()
            MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.new().content_type_associated_factories[factory.get_valid_content_type(), factory]
        rescue => exception
            raise exception
        end
      end
    end
  end
  