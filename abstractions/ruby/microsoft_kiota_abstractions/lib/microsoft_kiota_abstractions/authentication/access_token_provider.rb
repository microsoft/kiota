# frozen_string_literal: true

require_relative 'allowed_hosts_validator'

module MicrosoftKiotaAbstractions
  # Access Token Provider Module implementation
  module AccessTokenProvider
    # This function obtains the authorization token.
    # :params
    #   uri: a string containing the uri 
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def get_authorization_token(uri, additional_properties = {})
      raise NotImplementedError.new
    end

    attr_accessor :scopes, :host_validator

  end
end
