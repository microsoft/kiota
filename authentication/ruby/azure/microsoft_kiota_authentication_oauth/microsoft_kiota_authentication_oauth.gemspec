# frozen_string_literal: true

Gem::Specification.new do |spec|
  spec.name          = "microsoft_kiota_authentication_oauth"
  spec.version       = "0.1.0"
  spec.authors       = 'Microsoft Corporation'
  spec.email         = 'graphsdkpub@microsoft.com'
  spec.description   = 'Kiota Authentication implementation with oauth2'
  spec.summary       = "Microsoft Kiota Authentication OAuth - Kiota Ruby Authentication OAuth library"
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
  
  spec.add_dependency 'concurrent-ruby', '~> 1.1', '>= 1.1.9'
  spec.add_dependency 'microsoft_kiota_abstractions'
  spec.add_dependency 'oauth2', '~> 2.0'
end
