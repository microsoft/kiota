# frozen_string_literal: true

require "integration_test"
require "microsoft_kiota_abstractions"
require "microsoft_kiota_faraday"
require "microsoft_kiota_serialization_json"
require "microsoft_kiota_authentication_oauth"

RSpec.configure do |config|
  # Enable flags like --only-failures and --next-failure
  config.example_status_persistence_file_path = ".rspec_status"

  # Disable RSpec exposing methods globally on `Module` and `main`
  config.disable_monkey_patching!

  config.expect_with :rspec do |c|
    c.syntax = :expect
  end
end
