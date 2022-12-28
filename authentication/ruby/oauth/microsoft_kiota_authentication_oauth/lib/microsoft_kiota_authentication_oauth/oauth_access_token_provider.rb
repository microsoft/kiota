# frozen_string_literal: true

require 'microsoft_kiota_abstractions'
require 'oauth2'
require_relative 'extensions/oauth2_ext'
require_relative 'contexts/client_credential_context'
require_relative 'contexts/authorization_code_context'
require_relative 'contexts/on_behalf_of_context'
require_relative 'contexts/oauth_context'
require_relative 'contexts/oauth_custom_flow'

module MicrosoftKiotaAuthenticationOAuth
  # Access Token Provider class implementation
  class OAuthAccessTokenProvider
    # This is the initializer for OAuthAccessTokenProvider.
    # :params
    #   token_request_context: a instance of one of our token request context or a custom implementation
    #   allowed_hosts: an array of strings, where each string is an allowed host, default is empty
    #   scopes: an array of strings, where each string is a scope, default is empty array 
    def initialize(token_request_context, allowed_hosts = [], scopes = [])
      raise StandardError, 'Parameter token_request_context cannot be nil.' if token_request_context.nil?

      @token_request_context = token_request_context

      unless @token_request_context.is_a?(MicrosoftKiotaAuthenticationOAuth::OAuthContext)
        raise StandardError, 'Parameter token_request_context must be an instance of one of our grant flow context classes.'
      end

      @cached_token = nil

      @host_validator = if allowed_hosts.nil? || allowed_hosts.size.zero?
                          MicrsoftKiotaAbstractions::AllowedHostsValidator.new(['graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
                                                     'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn',
                                                     'canary.graph.microsoft.com'])
                        else
                          MicrosoftKiotaAbstractions::AllowedHostsValidator.new(allowed_hosts)
                        end
      @token_request_context.initialize_oauth_provider
      @token_request_context.initialize_scopes(scopes)
    end

    # This function obtains the authorization token.
    # :params
    #   uri: a string containing the uri 
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def get_authorization_token(uri, additional_properties = {})
      return nil if !uri || !@host_validator.url_host_valid?(uri)

      parsed_url = URI(uri)

      raise StandardError, 'Only https is supported' if parsed_url.scheme != 'https'

      Fiber.new do
        if @cached_token
          token = OAuth2::AccessToken.from_hash(@token_request_context.oauth_provider, @cached_token) 
          return token.token if !token.nil? && !token.expired?

          if token.expired?
            token = token.refresh!
            @cached_token = token.to_hash
            return token.token
          end
        end

        token = nil
        token = @token_request_context.get_token

        @cached_token = token.to_hash unless token.nil?
        return token.token unless token.nil?
      end
    end

    attr_reader :scopes, :host_validator
    
    protected

    attr_writer :host_validator, :token_credential, :scopes, :cached_token

  end
end
