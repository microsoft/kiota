require 'concurrent'

module MicrosoftKiotaAbstractions
 class BaseBearerTokenAuthenticationProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async

    AUTHORIZATION_HEADER_KEY = 'Authorization'
    def initialize()

    end
    
    def authenticate_request(request)
      if !request
        raise StandardError, 'request cannot be null'
      end
      if !request.headers.has_key?(AUTHORIZATION_HEADER_KEY) 
        token = self.await.get_authorization_token(request.uri).value
        if !token
          raise StandardError, 'Could not get an authorization token'
        end
        if !request.headers
          request.headers Hash.new()
          end
        request.headers[AUTHORIZATION_HEADER_KEY] = 'Bearer ' + token
      end
    end

    def get_authorization_token(request)
      raise NotImplementedError.new
    end
      
  end
end
  