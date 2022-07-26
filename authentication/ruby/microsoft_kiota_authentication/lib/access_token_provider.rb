# frozen_string_literal: true

require 'concurrent'
require 'oauth2'
require_relative 'extensions/oauth2_ext'
require_relative 'allowed_hosts_validator'
require_relative 'contexts/client_credential_context'
require_relative 'contexts/authorization_code_context'
require_relative 'contexts/on_behalf_of_context'

module MicrosoftKiotaAuthentication
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
      raise StandardError, 'Parameter token_request_context cannot be nil.' if token_request_context.nil?

      @cached_token = nil 

      @token_request_context = token_request_context
      @host_validator = if allowed_hosts.nil? || allowed_hosts.size.zero?
                          AllowedHostsValidator.new(['graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
                                                     'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn',
                                                     'canary.graph.microsoft.com'])
                        else
                          AllocatedHostsValidator.new(allowed_hosts)
                        end
      
      @oauth_provider = init_oauth_provider
      @scopes = init_scopes(scopes)
      
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
        token = OAuth2::AccessToken.from_hash(@oauth_provider, @cached_token) 
        return token.token if !token.nil? && !token.expired?

        if token.expired?
          token = token.refresh!
          @cached_token = token.to_hash
          return token.token
        end
      end

      token = nil

      if @token_request_context.instance_of?(MicrosoftKiotaAuthentication::ClientCredentialContext)
        token = client_credential_get_authorization_token
      elsif @token_request_context.instance_of?(MicrosoftKiotaAuthentication::AuthorizationCodeContext)
        token = auth_code_get_authorization_token
      elsif @token_request_context.instance_of?(MicrosoftKiotaAuthentication::OnBehalfOf)
        token = on_behalf_of_get_authorization_token
      else
        raise StandardError, 'token_request_context must be an instance of one of our grant flow context classes.'
      end
      @cached_token = token.to_hash unless token.nil?
      token = token.token unless token.nil?
      token
    end

    attr_reader :scopes, :host_validator
    
    protected

    attr_writer :host_validator, :token_credential, :scopes, :cached_token

    def on_behalf_of_get_authorization_token(additional_properties = {})
      params = {
        grant_type: @token_request_context.grant_type,
        assertion: @token_request_context.assertion, 
        scope: @scopes,
        requested_token_use: 'on_behalf_of'
      }
      @oauth_provider.on_behalf_of.get_token(params)
    end
   
    def client_credential_get_authorization_token(additional_properties = {})
      @oauth_provider.client_credentials.get_token({ scope: @scopes })
    end

    def auth_code_get_authorization_token(additional_properties = {})
      @oauth_provider.auth_code.get_token(@token_request_context.auth_code, redirect_uri: @token_request_context.redirect_uri)
    end

    # This function initializes the oauth_provider
    def init_oauth_provider
      if (@token_request_context.instance_of? MicrosoftKiotaAuthentication::ClientCredentialContext) ||
          (@token_request_context.instance_of? MicrosoftKiotaAuthentication::AuthorizationCodeContext)
        OAuth2::Client.new(@token_request_context.client_id, 
                            @token_request_context.client_secret, 
                            site: 'https://login.microsoftonline.com',
                            authorize_url: "/#{@token_request_context.tenant_id}/oauth2/v2.0/authorize",
                            token_url: "/#{@token_request_context.tenant_id}/oauth2/v2.0/token")
      else
        raise StandardError, 'token_request_context must be an instance of one of our grant flow context classes.'
      end
    end

    # This function initializes the scopes for the access token provider
    def init_scopes(scopes)
      if (scopes.nil? || scopes.empty?) && ((@token_request_context.instance_of? MicrosoftKiotaAuthentication::AuthorizationCodeContext) || 
         (@token_request_context.instance_of? MicrosoftKiotaAuthentication::OnBehalfOfContext))
        raise StandardError, 'Parameter scopes cannot be nil/empty while using auth code or on behalf of grant type.' 
      end

      if (@token_request_context.instance_of? MicrosoftKiotaAuthentication::AuthorizationCodeContext) || 
        (@token_request_context.instance_of? MicrosoftKiotaAuthentication::OnBehalfOfContext)
        scopes.unshift('offline_access')
      end 

      scope_str = ''
      scopes.each { |scope| scope_str += scope + ' '}
      scope_str = 'https://graph.microsoft.com/.default' if scope_str.empty?
      scope_str
    end
  end
end
