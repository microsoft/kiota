require 'microsoft_kiota_abstractions'
class ParseNodeFactoryMock
    include MicrosoftKiotaAbstractions::ParseNodeFactory

    def get_valid_content_type
      'application/json'
    end
    def get_parse_node(clean_content_type, content)
        return {}
    end
end