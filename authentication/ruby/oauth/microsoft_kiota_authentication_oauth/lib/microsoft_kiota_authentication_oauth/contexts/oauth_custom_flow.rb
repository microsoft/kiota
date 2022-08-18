# frozen_string_literal: true

require 'oauth2'

module MicrosoftKiotaAuthenticationOAuth
    # Module that can be optionally implemented for supporting custom token grant flows.
    # To use a cutsom token grant flow, implement the functions below and 
    # use MicrosoftKiotaAuthenticationOAuth::OAuthContext.new as your token_request_context
    # object for the use by the MicrosoftKiotaAuthenticationOAuth::OAuthAccessTokenProvider
    module OAuthCustomFlow
        # Function that returns an oauth client using the oauth2 gem 
        def self.get_oauth_provider
            raise NotImplementedError.new
        end

        # Function that returns a space seperated string of scopes, beginning with 
        # the offline_access scope if relevant
        def self.get_scopes
            raise NotImplementedError.new
        end

        # Function that returns the access token 
        def self.get_token 
            raise NotImplementedError.new
        end
    end
end