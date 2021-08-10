require 'microsoft_kiota_abstractions'
require 'json'

module MicrosoftKiotaSerialization
  class JsonParseNodeFactory
    include MicrosoftKiotaAbstractions::ParseNodeFactory

    def get_valid_content_type
      'application/json'
    end

    def get_parse_node(content_type, content)
      if !content_type
        raise StandardError, 'content type cannot be undefined or empty'
      elsif get_valid_content_type != content_type
        raise StandardError, `expected a #{get_valid_content_type} content type`
      end
      raise StandardError, 'content cannot be undefined or empty' unless content

      JsonParseNode.new(JSON.parse(content))
    end
  end
end
