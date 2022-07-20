# frozen_string_literal: true

require 'concurrent'
require_relative './authentication_provider'
require_relative './access_token_provider'

module MicrosoftKiotaAbstractions
  # Provides a base class for implementing AuthenticationProvider for Bearer token scheme
  class BaseBearerTokenAuthenticationProvider
    include Concurrent::Async

    def initialize(access_token_provider)
      raise NotImplementedError.new
    end 

    AUTHORIZATION_HEADER_KEY = 'Authorization'
    def authenticate_request(request, additional_properties)
      raise NotImplementedError.new
    end

  end
end
