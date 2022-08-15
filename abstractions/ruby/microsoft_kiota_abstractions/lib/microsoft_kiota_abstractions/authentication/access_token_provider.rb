# frozen_string_literal: true

require 'concurrent'
require_relative 'allowed_hosts_validator'

module MicrosoftKiotaAbstractions
  # Access Token Provider class implementation
  class AccessTokenProvider
    include Concurrent::Async
    # This is the initializer for AccessTokenProvider.
    # :params
    #   token_request_context: a instance of one of our token request context
    #   allowed_hosts: an array of strings, where each string is an allowed host, default is empty
    #   scopes: an array of strings, where each string is a scope, default is empty array 
    #   auth_code: a string containting the auth code; default is nil, can be updated post-initialization
    def initialize(allowed_hosts = [], scopes = [])
      base_constructor = nil 
      @host_validator = if allowed_hosts.nil? || allowed_hosts.size.zero?
                          MicrsoftKiotaAbstractions::AllowedHostsValidator.new(['graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
                                                                                'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn',
                                                                                'canary.graph.microsoft.com'])
                        else
                          MicrsoftKiotaAbstractions::AllocatedHostsValidator.new(allowed_hosts)
                        end
    end

    # This function obtains the authorization token.
    # :params
    #   uri: a string containing the uri 
    #   additional_params: hash of symbols to string values, ie { response_mode: 'fragment', prompt: 'login' }
    #                      default is empty hash
    def get_authorization_token(uri, additional_properties = {})
      raise NotImplementedError.new
    end

    attr_reader :scopes, :cached_token, :host_validator
    
    private

    attr_writer :host_validator, :token_credential, :scopes

  end
end
