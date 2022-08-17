# frozen_string_literal: true

require 'oauth2'
require_relative './oauth_context'

module MicrosoftKiotaAuthenticationOAuth
  # Token request context class for the authorization code grant type.
  class AuthorizationCodeContext < MicrosoftKiotaAuthenticationOAuth::OAuthContext
    attr_reader :grant_type, :redirect_uri, :additional_params,
                :tenant_id, :client_id, :client_secret, :auth_code, :oauth_provider
    attr_writer :scopes

    # This is the initializer for AuthorizationCodeContext, the token request context when
    # using the authorization code grant flow. 
    # :params
    #   tenant_id: a string containing the tenant id 
    #   client_id: a string containing the client id 
    #   client_secret: a string containing the client secret
    #   redirect_uri: a string containing redirect_uri
    #   auth_code: a string containting the auth code; default is nil, can be updated post-initialization
    def initialize(tenant_id, client_id, client_secret, redirect_uri, auth_code = nil)
      raise StandardError, 'redirect_uri cannot be nil/empty' if redirect_uri.nil? || redirect_uri.empty?

      @tenant_id = tenant_id
      @client_id = client_id
      @client_secret = client_secret
      @auth_code = auth_code
      @redirect_uri = redirect_uri
      @scopes = nil
      @oauth_provider = nil
      @grant_type = 'authorization code'

      if @tenant_id.nil? || @client_id.nil? || @client_secret.nil? || @tenant_id.empty? || @client_id.empty? || @client_secret.empty?
        raise StandardError, 'tenant_id, client_id, and client_secret cannot be empty'
      end
    end

    # setter for auth_code
    def auth_code=(code)
      raise StandardError, 'auth_code cannot be empty/nil.' if code.nil? || code.empty?

      @auth_code = code
    end

    # This function generates an authorize URL for obtaining the auth code.
    # :params
    #   scopes: an array of stings, where each string is a scope
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def generate_authorize_url(scopes, additional_params = {})
      @additional_params = additional_params
      
      self.initialize_scopes(scopes)
      self.initialize_oauth_provider

      parameters = { scope: @scopes, redirect_uri: @redirect_uri, access_type: 'offline', prompt: 'consent'}
      parameters = parameters.merge(additional_params)
      @oauth_provider.auth_code.authorize_url(parameters)
    end

    def get_token
      @oauth_provider.auth_code.get_token(@auth_code, redirect_uri: @redirect_uri)
    end

    def initialize_oauth_provider
      @oauth_provider = OAuth2::Client.new(@client_id, @client_secret,
                                           site: 'https://login.microsoftonline.com',
                                           authorize_url: "/#{@tenant_id}/oauth2/v2.0/authorize",
                                           token_url: "/#{@tenant_id}/oauth2/v2.0/token")
    end

    def initialize_scopes(scopes)
      scope_str = ''
      scopes.each { |scope| scope_str += scope + ' '}
      raise StandardError, 'scopes cannot be empty/nil.' if scope_str.empty?
      
      scope_str = 'offline_access ' + scope_str

      @scopes = scope_str
    end

    private

    attr_writer :grant_type, :redirect_uri, :additional_params,
                :tenant_id, :client_id, :client_secret, :oauth_provider
  end
end
