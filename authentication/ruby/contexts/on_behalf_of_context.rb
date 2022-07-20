# frozen_string_literal: true

module MicrosoftKiotaAbstractions
    # Token request context class for the on behlaf of grant type.
    class OnBehalfOfContext
        attr_reader :grant_type, :additional_params, :tenant_id, :client_id, :client_secret

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
          @grant_type = 'urn:ietf:params:Oauth:grant-type:jwt-bearer'
    
          if tenant_id.nil? || client_id.nil? || client_secret.nil? || client_secret.empty? || tenant_id.empty? || client_id.empty?
            raise StandardError, 'tenant_id, client_secret, and client_id cannot be empty'
          end

        end
    end
end
