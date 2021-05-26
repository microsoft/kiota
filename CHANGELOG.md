# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Adds kiota packaging as a dotnet tool #169
- Adds input parameters validation #168
- Expands code coverage to 88% #147
- Removes json assumption for request body to support multiple formats #170
- Escapes language reserved keywords #184
- Replaces custom URL tree node by class provided by OpenAPI.net #179

## [0.0.4] - 2021-04-28

### Changed

- Multiple performance improvements for large descriptions
- Deterministic ordering of properties/methods/indexers/subclasses
- Deterministic import of sub path request builders
- Stopped generating phantom indexer methods for TypeScript and Java
- Fixed a bug where prefixed properties would be missing their prefix for serialization

## [0.0.3] - 2021-04-25

### Added

- Adds supports for additional properties in models

## [0.0.2] - 2021-04-20

### Added

- CI/CD to docker image (private feed) and GitHub releases #112, #115
- Documentation to get started
- Published the core packages #110
- Factories support for serializers and deserializers #100
- Documentation comment generation #92
- Submodule with generation samples #106
- Test coverage information in sonarcloud #78

### Changed

- Fixed a bug where date time offset properties would not be generated properly #116
- Fixed a bug where generating from http/https OpenAPI description would fail #109
- Fixed a bug where simple schema references would not be handled #109
- Removed a dependency on operation id #89
- Fixed a bug where the sonarcloud workflow would fail on external PRs #102
- Fixed a bug where empty class names would fail the generation #88

## [0.0.1] - 2021-04-20

### Added

- Initial GitHub release
