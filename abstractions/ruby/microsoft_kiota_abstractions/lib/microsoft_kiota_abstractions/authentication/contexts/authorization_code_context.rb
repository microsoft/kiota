# frozen_string_literal: true

require 'oauth2'

module MicrosoftKiotaAbstractions
  # Token request context class for the authorization code grant type.
  class AuthorizationCodeContext
    attr_reader :grant_type, :redirect_uri, :additional_params,
                :tenant_id, :client_id, :client_secret, :auth_code

    # This is the initializer for AuthorizationCodeContext, the token request context when
    # using the authorization code grant flow. 
    # :params
    #   tenant_id: a string containing the tenant id 
    #   client_id: a string containing the client id 
    #   client_secret: a string containing the client secret
    #   redirect_uri: a string containing redirect_uri
    #   auth_code: a string containting the auth code; default is nil, can be updated post-initialization
    def initialize(tenant_id, client_id, client_secret, redirect_uri, auth_code = nil)
      raise NotImplementedError.new
    end

    # setter for auth_code
    def auth_code=(code)
      raise NotImplementedError.new
    end

    # This function generates an authorize URL for obtaining the auth code.
    # :params
    #   scopes: an array of stings, where each string is a scope
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def generate_authorize_url(scopes, additional_params = {})
      raise NotImplementedError.new

    end

    private

    attr_writer :grant_type, :redirect_uri, :additional_params,
                :tenant_id, :client_id, :client_secret
  end
end
