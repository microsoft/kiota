# frozen_string_literal: true

require_relative "lib/microsoft_kiota_serialization/version"

Gem::Specification.new do |spec|
  spec.name          = "microsoft_kiota_serialization"
  spec.version       = MicrosoftKiotaSerialization::VERSION
  spec.authors       = 'Microsoft Corporation'
  spec.email         = 'graphsdkpub@microsoft.com'
  spec.description   = 'Implementation of Kiota Serialization interfaces for JSON'
  spec.summary       = 'Microsoft Kiota Serialization - Ruby serialization for building library agnostic http client'
  spec.homepage      = 'https://microsoft.github.io/kiota/'
  spec.license       = 'MIT'
  spec.metadata      = {
    'bug_tracker_uri' => 'https://github.com/microsoft/kiota/issues',
    'changelog_uri'   => 'https://github.com/microsoft/kiota/blob/main/CHANGELOG.md',
    'homepage_uri'    => spec.homepage,
    'source_code_uri' => 'https://github.com/microsoft/kiota',
    'github_repo'     => 'ssh://github.com/microsoft/kiota'
  }
  spec.required_ruby_version = ">= 2.4.0"

  # Specify which files should be added to the gem when it is released.
  # The `git ls-files -z` loads the files in the RubyGem that have been added into git.
  spec.files = Dir.chdir(File.expand_path(__dir__)) do
    `git ls-files -z`.split("\x0").reject { |f| f.match(%r{\A(?:test|spec|features)/}) }
  end
  spec.bindir        = 'bin'
  spec.executables   = spec.files.grep(%r{\Aexe/}) { |f| File.basename(f) }
  spec.require_paths = ['lib']

  spec.add_dependency 'uuidtools'
end
