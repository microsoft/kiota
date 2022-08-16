require "microsoft_kiota_abstractions"

module MicrosoftKiotaAuthentication
  class OAuthAuthenticationProvider < MicrosoftKiotaAbstractions::BaseBearerTokenAuthenticationProvider
    def initialize(token_request_context, allowed_hosts, scopes)
      super(MicrosoftKiotaAuthentication::OAuthAccessTokenProvider.new(token_request_context, allowed_hosts, scopes))
    end
  end
end
