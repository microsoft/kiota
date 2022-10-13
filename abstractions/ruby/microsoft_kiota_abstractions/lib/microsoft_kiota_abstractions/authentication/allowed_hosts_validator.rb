# frozen_string_literal: true

require 'uri'

module MicrosoftKiotaAbstractions
  # Maintains a list of valid hosts and allows authentication providers to check whether
  # a host is valid before authenticating a request
  class AllowedHostsValidator
    # creates a new AllocatedHostsValidator with provided values
    def initialize(allowed_hosts)
      @allowed_hosts = {}
      allowed_hosts.each { |host| @allowed_hosts[host.downcase] = true }
    end

    # sets the list of valid hosts with provided value (val)
    def allowed_hosts=(val)
      @allowed_hosts = {}
      val.each { |host| @allowed_hosts[host.downcase] = true }
    end

    # checks whether the provided host is valid
    def url_host_valid?(url)
      return true if @allowed_hosts.empty?

      parsed_url = URI(url)
      return false if parsed_url.host.nil?
      
      return false unless parsed_url.is_a?(URI::HTTPS)

      @allowed_hosts.key? parsed_url.host.downcase

    rescue URI::InvalidURIError
      false
    end

    # gets the list of valid hosts
    attr_reader :allowed_hosts
  end
end
