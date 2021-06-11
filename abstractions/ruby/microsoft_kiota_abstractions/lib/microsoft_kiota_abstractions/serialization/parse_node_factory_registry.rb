require_relative 'parse_node_factory'

module MicrosoftKiotaAbstractions
    class ParseNodeFactoryRegistry
        include ParseNodeFactory

        def content_type_associated_factories
            @content_type_associated_factories ||= Hash.new
        end

        def get_parse_node(content_type, content)
            if !content_type
                raise Exception.new "content type cannot be undefined or empty"
            end
            if !content
                raise Exception.new "content cannot be undefined or empty"
            end
            factory = @content_type_associated_factories[content_type]
            if factory
                return factory.get_parse_node(content_type, content)
            else
                raise Exception.new "Content type #{contentType} does not have a factory to be parsed"
            end
        end
        
    end
end
