# frozen_string_literal: true

require 'oauth2'
require_relative './oauth_context'

module MicrosoftKiotaAuthenticationOAuth
  # Token request context class for the client credential grant type.
  class ClientCredentialContext < MicrosoftKiotaAuthenticationOAuth::OAuthContext
    attr_reader :grant_type, :additional_params, :tenant_id, :client_id, :client_secret, :oauth_provider
    attr_writer :scopes 

    # This is the initializer for ClientCredentialContext, the token request context when
    # using the client credential grant flow. 
    # :params
    #   tenant_id: a string containing the tenant id 
    #   client_id: a string containing the client id 
    #   client_secret: a string containing the client secret
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def initialize(tenant_id, client_id, client_secret, additional_params = {})
      @tenant_id = tenant_id
      @client_id = client_id
      @client_secret = client_secret
      @additional_params = additional_params
      @scopes = nil
      @oauth_provider = nil
      @grant_type = 'client credential'


      if @tenant_id.nil? || @client_id.nil? || @client_secret.nil? || @tenant_id.empty? || @client_id.empty? || @client_secret.empty?
        raise StandardError, 'tenant_id, client_id and client_secret cannot be empty'
      end
    end

    def get_token
      @oauth_provider.client_credentials.get_token({ scope: @scopes })
    end

    def initialize_oauth_provider
      @oauth_provider = OAuth2::Client.new(@client_id, @client_secret,
                                            site: 'https://login.microsoftonline.com',
                                            authorize_url: "/#{@tenant_id}/oauth2/v2.0/authorize",
                                            token_url: "/#{@tenant_id}/oauth2/v2.0/token")
    end

    # Function to initialize the scope for the client credential context object.
    # This function forces to default since gradual consent is not supported 
    # for this flow.
    def initialize_scopes(scopes = []) 
      scope_str = 'https://graph.microsoft.com/.default'
      @scopes = scope_str
    end


    private
    
    attr_writer :grant_type, :additional_params, :tenant_id, :client_id, :client_secret, :oauth_provider
  end
end
