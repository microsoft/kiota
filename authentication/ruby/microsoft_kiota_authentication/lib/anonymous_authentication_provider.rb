require 'concurrent'

module MicrosoftKiotaAuthentication
  class AnonymousAuthenticationProvider
    include MicrosoftKiotaAuthentication::AuthenticationProvider
    include Concurrent::Async
    def authenticate_request(request)
    end
  end
end