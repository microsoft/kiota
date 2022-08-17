# frozen_string_literal: true

require 'concurrent'
require_relative './authentication_provider'
require_relative './access_token_provider'

module MicrosoftKiotaAbstractions
  # Provides a base class for implementing AuthenticationProvider for Bearer token scheme
  class BaseBearerTokenAuthenticationProvider
    include MicrosoftKiotaAbstractions::AccessTokenProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async
    def initialize(access_token_provider)
      raise StandardError, 'access_token_provider parameter cannot be nil' if access_token_provider.nil?

      @access_token_provider = access_token_provider
    end 

    AUTHORIZATION_HEADER_KEY = 'Authorization'
    def authenticate_request(request, additional_properties)
      raise StandardError, 'Request cannot be null' if request.nil?
      return if request.headers.key?(AUTHORIZATION_HEADER_KEY)

      token = @access_token_provider.get_authorization_token(request, additional_properties)

      request.headers[AUTHORIZATION_HEADER_KEY] = "Bearer #{token}" unless token.nil?
    end
  end
end
