# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
