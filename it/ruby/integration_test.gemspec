# frozen_string_literal: true

require_relative "lib/integration_test/version"

Gem::Specification.new do |spec|
  spec.name = "integration_test"
  spec.version = Integration_test::VERSION
  spec.authors = ["Microsoft Graph DevX"]
  spec.email = ["graphsdkpub+ruby@microsoft.com"]

  spec.summary = "integration tests project"
  spec.description = "integration tests project"
  spec.homepage = "https://learn.microsoft.com/openapi/kiota"
  spec.required_ruby_version = ">= 3.0.0"

  spec.metadata["homepage_uri"] = spec.homepage
  spec.metadata["source_code_uri"] = "https://github.com/microsoft/kiota"
  spec.metadata["changelog_uri"] = "https://github.com/microsoft/kiota/tree/main/CHANGELOG.md"

  # Specify which files should be added to the gem when it is released.
  # The `git ls-files -z` loads the files in the RubyGem that have been added into git.
  spec.files = Dir.chdir(__dir__) do
    `git ls-files -z`.split("\x0").reject do |f|
      (f == __FILE__) || f.match(%r{\A(?:(?:bin|test|spec|features)/|\.(?:git|circleci)|appveyor)})
    end
  end
  spec.bindir = "exe"
  spec.executables = spec.files.grep(%r{\Aexe/}) { |f| File.basename(f) }
  spec.require_paths = ["lib"]

  # Uncomment to register a new dependency of your gem
  # spec.add_dependency "example-gem", "~> 1.0"

  # For more information and examples about making a new gem, check out our
  # guide at: https://bundler.io/guides/creating_gem.html
end
