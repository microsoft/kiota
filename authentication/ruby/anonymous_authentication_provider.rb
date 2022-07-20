require 'concurrent'

module MicrosoftKiotaAbstractions
  class AnonymousAuthenticationProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async
    def authenticate_request(request)
    end
  end
end