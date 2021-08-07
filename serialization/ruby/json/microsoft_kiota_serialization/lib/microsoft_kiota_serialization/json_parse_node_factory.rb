require 'microsoft_kiota_abstractions'
require 'json'

module MicrosoftKiotaSerialization
    class JsonParseNodeFactory
        include MicrosoftKiotaAbstractions::ParseNodeFactory

        def get_valid_content_type()
            return "application/json"
        end

        def get_parse_node(content_type, content)
            if !content_type
				raise Exception.new 'content type cannot be undefined or empty'
            elsif self.get_valid_content_type() != content_type
                raise Exception.new `expected a #{self.get_valid_content_type()} content type`
            end
            if !content
				raise Exception.new 'content cannot be undefined or empty'
			end
            return JsonParseNode.new(JSON.parse(content))
        end
    end
end
