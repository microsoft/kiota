# frozen_string_literal: true

require 'oauth2'

module MicrosoftKiotaAuthenticationOAuth
    # Base class token request context and can be optionally implemented for 
    # custom token grant flows.
    class OAuthContext
      attr_accessor :scopes
      attr_reader :oauth_provider

      # oauth client
      @oauth_provider 
      # scopes
      @scopes
      def initialize
        raise NotImplementedError.new
      end

      def get_token
        raise NotImplementedError.new
      end

      def initialize_scopes(scopes = [])
        raise NotImplementedError.new
      end

      def initialize_oauth_provider
        raise NotImplementedError.new
      end

      private 

      attr_writer :oauth_provider

    end
end