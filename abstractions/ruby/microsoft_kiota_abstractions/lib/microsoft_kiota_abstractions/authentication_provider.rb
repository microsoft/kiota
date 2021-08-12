require 'concurrent'

module MicrosoftKiotaAbstractions
  module AuthenticationProvider
    include Concurrent::Async

    def get_authorization_token(request_url)
      raise NotImplementedError.new
    end
    
  end
end
