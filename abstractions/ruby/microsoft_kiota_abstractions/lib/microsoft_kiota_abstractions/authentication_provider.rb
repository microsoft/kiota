module MicrosoftKiotaAbstractions
    module AuthenticationProvider
        def get_authorization_token(request_url)
            raise NotImplementedError.new
        end
    end
end
