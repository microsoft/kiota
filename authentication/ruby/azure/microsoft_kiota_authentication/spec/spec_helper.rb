# frozen_string_literal: true

require 'microsoft_kiota_abstractions'
require_relative '../contexts/client_credential_context'
require_relative '../contexts/authorization_code_context'
require_relative '../contexts/on_behalf_of_context'
require_relative '../extensions/oauth2_ext'
require_relative '../azure_access_token_provider'

RSpec.configure do |config|
  # Enable flags like --only-failures and --next-failure
  config.example_status_persistence_file_path = ".rspec_status"

  # Disable RSpec exposing methods globally on `Module` and `main`
  config.disable_monkey_patching!

end
