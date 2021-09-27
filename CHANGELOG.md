# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

- Fixes a bug where empty title would make generation fail #558
- Fixes a bug where float, long and binary types would not be parsed by the generator #558
- Fixes a bug where generation would fail on compact namespace names #558
- Renames request info into request information to avoid conflicts with platform #559
- Fixes a bug where the server url would not be taken in consideration #626

## [0.0.8] - 2021-08-25

### Added

- Ruby JSON serialization #429
- Ruby HTTP service #472
- Go generation support & abstractions #413

### Changed

- Fixed a bug where raw collections requests would not be supported #467
- Fixes a bug where in memory backing store would not return changed properties to null #243
- Fixes a bug where generated models would be tied to a specific backing store implementation #400
- Fixed #428 a bug where inline double defintion would make code dom generation fail
- Revamped authentication provider interface to allow multiple authentication schemes #498
- Fixed a bug preventing from using request builders with raw URls #508

## [0.0.7] - 2021-08-04

### Added

- Ruby generation implemented #244
- Adds middleware support for http clients #330

## [0.0.6] - 2021-07-26

### Added

- Initial ruby abstractions #212
- Backing store support #223
- Doc comments for abstractions libraries #324

### Changed

- Better client configuration #268
- Request builders constructors for data validation #322

## [0.0.5] - 2021-06-10

### Changed

- Expands code coverage to 88% #147
- Removes json assumption for request body to support multiple formats #170
- Escapes language reserved keywords #184
- Replaces custom URL tree node by class provided by OpenAPI.net #179
- Splits the core libraries in 3 separate libraries #197
- Changes default namespace and class name to api client #199
- Aligns Parsable interfaces across languages #204
- Fixes a bug where classes with properties of identical name would make build fail in CSharp #222

### Added

- Adds kiota packaging as a dotnet tool #169
- Adds input parameters validation #168
- Adds support for collections as root responses #191

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
