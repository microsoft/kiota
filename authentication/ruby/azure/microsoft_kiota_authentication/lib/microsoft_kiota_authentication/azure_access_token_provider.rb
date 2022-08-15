# frozen_string_literal: true

require 'concurrent'
require 'microsoft_kiota_abstractions'
require 'oauth2'
require_relative 'extensions/oauth2_ext'
require_relative 'contexts/client_credential_context'
require_relative 'contexts/authorization_code_context'
require_relative 'contexts/on_behalf_of_context'

module MicrosoftKiotaAuthentication
  # Access Token Provider class implementation
  class AzureAccessTokenProvider
    include Concurrent::Async
    # This is the initializer for AzureAccessTokenProvider.
    # :params
    #   token_request_context: a instance of one of our token request context
    #   allowed_hosts: an array of strings, where each string is an allowed host, default is empty
    #   scopes: an array of strings, where each string is a scope, default is empty array 
    #   auth_code: a string containting the auth code; default is nil, can be updated post-initialization
    def initialize(token_request_context, allowed_hosts = [], scopes = [])
      raise StandardError, 'Parameter token_request_context cannot be nil.' if token_request_context.nil?

      if !@token_request_context.instance_of?(MicrosoftKiotaAuthentication::ClientCredentialContext) && 
         !@token_request_context.instance_of?(MicrosoftKiotaAuthentication::AuthorizationCodeContext) &&
         !@token_request_context.instance_of?(MicrosoftKiotaAuthentication::OnBehalfOf)
        raise StandardError, 'Parameter token_request_context must be an instance of one of our grant flow context classes.'
      end

      @cached_token = nil 

      @token_request_context = token_request_context
      @host_validator = if allowed_hosts.nil? || allowed_hosts.size.zero?
                          MicrsoftKiotaAbstractions::AllowedHostsValidator.new(['graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
                                                     'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn',
                                                     'canary.graph.microsoft.com'])
                        else
                          MicrsoftKiotaAbstractions::AllocatedHostsValidator.new(allowed_hosts)
                        end
      
      token_request_context.initialize_oauth_provider
      token_request_context.initialize_scopes(scopes)
      
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

      unless cached_token.nil?
        token = OAuth2::AccessToken.from_hash(@token_request_context.oauth_provider, @cached_token) 
        return token.token if !token.nil? && !token.expired?

        if token.expired?
          token = token.refresh!
          @cached_token = token.to_hash
          return token.token
        end
      end

      token = nil
      token = token_request_context.get_token

      @cached_token = token.to_hash unless token.nil?
      token = token.token unless token.nil?
      token
    end

    attr_reader :scopes, :host_validator
    
    protected

    attr_writer :host_validator, :token_credential, :scopes, :cached_token

  end
end
