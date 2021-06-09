# frozen_string_literal: true

require_relative "lib/microsoft_kiota_abstractions/version"

Gem::Specification.new do |spec|
  spec.name          = "microsoft_kiota_abstractions"
  spec.version       = MicrosoftKiotaAbstractions::VERSION
  spec.authors       = ["Microsoft"] # Todo: required
  # spec.email         = ["TODO: Write your email address - not required"]

  # TODO: required
  spec.summary       = "The Kiota abstractions are language specific libraries defining the basic constructs Kiota projects need once an SDK has been generated from an OpenAPI definition." 
  # spec.description   = "TODO: Write a longer description or delete this line. - not required" 
  spec.homepage      = "https://github.com/microsoft/kiota"
  spec.required_ruby_version = ">= 2.4.0"

  spec.metadata["allowed_push_host"] = "TODO: Set to 'http://mygemserver.com'"

  spec.metadata["homepage_uri"] = spec.homepage
  spec.metadata["github_repo"] = "ssh://github.com/microsoft/kiota"
  spec.metadata["source_code_uri"] = "https://github.com/microsoft/kiota"
  spec.metadata["changelog_uri"] = "https://github.com/microsoft/kiota/blob/main/CHANGELOG.md"

  # Specify which files should be added to the gem when it is released.
  # The `git ls-files -z` loads the files in the RubyGem that have been added into git.
  spec.files = Dir.chdir(File.expand_path(__dir__)) do
    `git ls-files -z`.split("\x0").reject { |f| f.match(%r{\A(?:test|spec|features)/}) }
  end
  spec.bindir        = "exe"
  spec.executables   = spec.files.grep(%r{\Aexe/}) { |f| File.basename(f) }
  spec.require_paths = ["lib"]

  # Uncomment to register a new dependency of your gem
  # spec.add_dependency "example-gem", "~> 1.0"

  spec.add_dependency "uri"

  # For more information and examples about making a new gem, checkout our
  # guide at: https://bundler.io/guides/creating_gem.html
end
