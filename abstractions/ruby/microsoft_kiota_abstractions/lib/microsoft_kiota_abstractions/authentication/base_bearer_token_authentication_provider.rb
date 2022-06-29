# frozen_string_literal: true

require 'concurrent'

module MicrosoftKiotaAbstractions
  # Provides a base class for implementing AuthenticationProvider for Bearer token scheme
  class BaseBearerTokenAuthenticationProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async

    AUTHORIZATION_HEADER_KEY = 'Authorization'
    def authenticate_request(request)
      raise StandardError, 'Request cannot be null' unless request
      return if request.headers.key?(AUTHORIZATION_HEADER_KEY)

      token = get_authorization_token(request)
      raise StandardError, 'Could not get an authorization token' unless token

      request.headers[AUTHORIZATION_HEADER_KEY] = "Bearer #{token}"
    end

    def get_authorization_token(request)
      raise NotImplementedError, 'get_authorization_token must be implemented'
    end
  end
end
