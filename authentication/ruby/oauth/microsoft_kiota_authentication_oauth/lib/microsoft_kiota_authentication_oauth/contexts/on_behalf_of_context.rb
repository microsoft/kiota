# frozen_string_literal: true

require 'oauth2'
require_relative './oauth_context'

module MicrosoftKiotaAuthenticationOAuth
    # Token request context class for the on behlaf of grant type.
    class OnBehalfOfContext < MicrosoftKiotaAuthenticationOAuth::OAuthContext
        attr_reader :grant_type, :additional_params, :tenant_id, :client_id, :client_secret, :oauth_provider
        attr_writer :scopes

        # This is the initializer for OnBehalfOfContext, the token request context when
        # using the client credential grant flow. 
        # :params
        #   tenant_id: a string containing the tenant id 
        #   client_id: a string containing the client id 
        #   client_secret: a string containing the client secret 
        #   assertion: string containing assertion (access token used in the request)
        #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
        #                      default is empty hash
        def initialize(tenant_id, client_id, client_secret, assertion, additional_params = {})
          raise StandardError, 'assertion cannot be empty' if assertion.nil? || assertion.empty?

          @tenant_id = tenant_id
          @client_id = client_id
          @client_secret = client_secret
          @assertion = assertion
          @additional_params = additional_params
          @scopes = nil
          @oauth_provider = nil
          @grant_type = 'urn:ietf:params:Oauth:grant-type:jwt-bearer'
    
          if @tenant_id.nil? || @client_id.nil? || @client_secret.nil? || @client_secret.empty? || @tenant_id.empty? || @client_id.empty?
            raise StandardError, 'tenant_id, client_secret, and client_id cannot be empty'
          end
        end

        def get_token
          params = {
            grant_type: @grant_type,
            assertion: @assertion, 
            scope: @scopes,
            requested_token_use: 'on_behalf_of'
          }
          @oauth_provider.on_behalf_of.get_token(params)
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

        attr_writer :grant_type, :additional_params, :tenant_id, :client_id, 
                    :client_secret, :oauth_provider
        
    end
end
