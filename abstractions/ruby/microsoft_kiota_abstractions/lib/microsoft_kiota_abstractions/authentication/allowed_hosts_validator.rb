# frozen_string_literal: true

require 'uri'

module MicrosoftKiotaAbstractions
  # Maintains a list of valid hosts and allows authentication providers to check whether
  # a host is valid before authenticating a request
  class AllowedHostsValidator
    # creates a new AllocatedHostsValidator with provided values
    def initialize(allowed_hosts)
      raise NotImplementedError.new
    end

    # sets the list of valid hosts with provided value (val)
    def allowed_hosts=(val)
      raise NotImplementedError.new
    end

    # checks whether the provided host is valid
    def url_host_valid?(url)
      raise NotImplementedError.new
    end

    # gets the list of valid hosts
    attr_reader :allowed_hosts
  end
end
