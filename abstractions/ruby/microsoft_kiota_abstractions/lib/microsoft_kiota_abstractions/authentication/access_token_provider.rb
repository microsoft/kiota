# frozen_string_literal: true

require 'concurrent'
require 'oauth2'
require_relative 'extensions/oauth2_ext'
require_relative 'allowed_hosts_validator'
require_relative 'contexts/client_credential_context'
require_relative 'contexts/authorization_code_context'
require_relative 'contexts/on_behalf_of_context'

module MicrosoftKiotaAbstractions
  # Access Token Provider class implementation
  class AccessTokenProvider
    include Concurrent::Async
    # This is the initializer for AccessTokenProvider.
    # :params
    #   token_request_context: a instance of one of our token request context
    #   allowed_hosts: an array of strings, where each string is an allowed host, default is empty
    #   scopes: an array of strings, where each string is a scope, default is empty array 
    #   auth_code: a string containting the auth code; default is nil, can be updated post-initialization
    def initialize(token_request_context, allowed_hosts = [], scopes = [])
      raise NotImplementedError.new
    end

    # This function obtains the authorization token.
    # :params
    #   uri: a string containing the uri 
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def get_authorization_token(uri, additional_properties = {})
      raise NotImplementedError.new
    end

    attr_reader :scopes, :cached_token, :host_validator
    
    private

    attr_writer :host_validator, :token_credential, :scopes

  end
end
