require 'microsoft_kiota_abstractions'
require 'json'

module MicrosoftKiotaSerialization
    class JsonParseNode
        include MicrosoftKiotaAbstractions::Parsable
        @current_node
        def initialize(node)
            @current_node = node
        end
    end
end