# frozen_string_literal: true

module MicrosoftKiotaAbstractions
  # Token request context class for the client credential grant type.
  class ClientCredentialContext
    attr_reader :grant_type, :additional_params, :tenant_id, :client_id, :client_secret

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
      @grant_type = 'client credential'

      if tenant_id.nil? || client_id.nil? || client_secret.nil? || tenant_id.empty? || client_id.empty? || client_secret.empty?
        raise StandardError, 'tenant_id, client_id and client_secret cannot be empty'
      end
    end

    private

    attr_writer :grant_type, :additional_params, :tenant_id, :client_id, :client_secret
  end
end
