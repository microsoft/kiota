module MicrosoftKiotaAbstractions
  class AnonymousAuthenticationProvider
      include MicrosoftKiotaAbstractions::AuthenticationProvider
      def authenticate_request(request)
      end
  end
end