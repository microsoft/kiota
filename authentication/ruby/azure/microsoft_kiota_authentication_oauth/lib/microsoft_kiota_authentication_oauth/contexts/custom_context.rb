# frozen_string_literal: true

require 'oauth2'

module MicrosoftKiotaAuthenticationOAuth
    # Class for optional custom token request context.
    class CustomContext
      attr_accessor :scopes
      attr_reader :oauth_provider

      def initialize()
        @oauth_provider
        @scopes
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