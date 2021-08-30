# MicrosoftKiotaAbstractions

![Ruby](https://github.com/microsoft/kiota/actions/workflows/abstractions-ruby.yml/badge.svg)

## Using the abstractions

Option 1: Bundler config

```shell
bundle config https://rubygems.pkg.github.com/microsoft/kiota USERNAME:TOKEN
```

Option 2: Configuring `~/.gemrc` file

```
---
:backtrace: false
:bulk_threshold: 1000
:sources:
- https://rubygems.org/
- https://USERNAME:TOKEN@rubygems.pkg.github.com/microsoft/kiota
:update_sources: true
:verbose: true  
```
## Installation

Add this line to your application's Gemfile:

```ruby
source "https://rubygems.pkg.github.com/microsoft" do
  gem "microsoft_kiota_abstractions", "0.1.1"
end
```

And then execute:

```shell
bundle install
```

Or install it yourself as:

 ```shell
 gem install microsoft_kiota_abstractions --version "0.1.1" --source "https://{USERNAME}{PASSWORD/TOKEN}rubygems.pkg.github.com/microsoft"
 ```

## Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/microsoft/kiota.
