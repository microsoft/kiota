# frozen_string_literal: true

require 'oauth2'
require_relative './oauth_custom_flow'

module MicrosoftKiotaAuthenticationOAuth
    # Base class for token request contexs.
    class OAuthContext
      attr_accessor :scopes
      attr_reader :oauth_provider
      include MicrosoftKiotaAuthenticationOAuth::OAuthCustomFlow

      def get_token
        OAuthCustomFlow.get_token
      end

      def initialize_scopes(scopes = [])
        @scopes = OAuthCustomFlow.get_scopes
      end

      def initialize_oauth_provider
        @oauth_provider = OAuthCustomFlow.get_oauth_provider
      end

      private 

      attr_writer :oauth_provider

    end
end