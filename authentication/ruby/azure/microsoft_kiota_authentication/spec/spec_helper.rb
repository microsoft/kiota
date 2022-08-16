# frozen_string_literal: true

require 'microsoft_kiota_abstractions'
require_relative '../lib/microsoft_kiota_authentication/contexts/client_credential_context'
require_relative '../lib/microsoft_kiota_authentication/contexts/authorization_code_context'
require_relative '../lib/microsoft_kiota_authentication/contexts/on_behalf_of_context'
require_relative '../lib/microsoft_kiota_authentication/contexts/custom_context'
require_relative '../lib/microsoft_kiota_authentication/extensions/oauth2_ext'
require_relative '../lib/microsoft_kiota_authentication/oauth_access_token_provider'
require_relative '../lib/microsoft_kiota_authentication/oauth_authentication_provider'

RSpec.configure do |config|
  # Enable flags like --only-failures and --next-failure
  config.example_status_persistence_file_path = ".rspec_status"

  # Disable RSpec exposing methods globally on `Module` and `main`
  config.disable_monkey_patching!

end
