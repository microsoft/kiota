module MicrosoftKiotaAbstractions
    module ParseNodeFactory
        def get_parse_node(content_type, content)
            raise NotImplementedError.new
        end
    end
end
