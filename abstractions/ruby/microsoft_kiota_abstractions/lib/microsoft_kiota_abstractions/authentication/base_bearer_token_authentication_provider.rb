require 'concurrent'

module MicrosoftKiotaAbstractions
 class BaseBearerTokenAuthenticationProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async

    AUTHORIZATION_HEADER_KEY = 'Authorization'
    def authenticate_request(request)
      if !request
        raise StandardError, 'request cannot be null'
      end
      if !request.headers.has_key?(AUTHORIZATION_HEADER_KEY) 
        token = self.get_authorization_token(request)
        if !token
          raise StandardError, 'Could not get an authorization token'
        end
        request.headers[AUTHORIZATION_HEADER_KEY] = 'Bearer ' + token
      end
    end

    def get_authorization_token(request)
      raise NotImplementedError, 'get_authorization_token must be implemented'
    end
  end
end
  