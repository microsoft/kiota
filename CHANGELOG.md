# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

- Add support for PHP Generation.
- Migrated generator to dotnet 6 #815
- Fixes a bug where json deserialization would fail in go
- Fixes a bug where query parameters would not be added to the request in go
- Fixes a bug where at signs in path would derail generation
- Fixes Go doc comments in packages and generation
- Fixes a bug where RequestInformation did not accept some content headers in dotnet
- Added support for providing cancellation token in dotnet #874, #875, #876
- Upgrades go libraries to go17.
- Fixes a bug in Go where reserved keywords for properties would be wrongly replaced.
- Fixes a bug in Go where setters would be missing nil checks.
- Fixes a bug where OData select query parameter would not be normalized
- Fixes a bug in Go where empty collections would not be serialized.

## [0.0.14] - 2021-11-08

### Added

- Added support for changing the base url #795

### Changed

- Fixes a bug where arrays of enums could be wrongly mapped.
- Fixes a bug where go deserialization would fail on collections of scalars.
- Fixes a bug where TypeScript query parameters would be added to headers instead #812
- Update dotnet abstractions and core libraries to target netstandard2.1 from net5.0

## [0.0.13] - 2021-10-27

### Changed

- Technical release to bump version number of go packages after replace removal

## [0.0.12] - 2021-10-27

### Added

- Adds Go authentication, http and serialization libraries and finalizes the generation #716

## [0.0.11] - 2021-10-27

### Changed

- Switched to URL templates instead of string contract for URL building #683
- Fixed a bug where CSharp method names would not follow naming conventions #730

## [0.0.10] - 2021-10-06

### Changed

- Renames middlewareoption into requestoption to stay agnostic from implementation #635
- Aligned http packages on naming convention #444

## [0.0.9] - 2021-10-01

### Added

- Adds support for path parameters #573
- Adds missing type mappings in TypeScript #573
- Adds a missing http core method for collections of primitives #573
- Aliases imports with the same name in typescript #573

### Changed

- Fixes a bug where empty title would make generation fail #558
- Fixes a bug where float, long and binary types would not be parsed by the generator #558
- Fixes a bug where generation would fail on compact namespace names #558
- Renames request info into request information to avoid conflicts with platform #559
- Fixes a bug where the server url would not be taken in consideration #626
- Fixes a bug where missing namespaces would make the generation fail #573
- Fixes a bug where class names could contain special characters #573
- Fixes a bug where namespace names could contain path parameters #573
- Fixes a bug where namespace names could contain special characters #573
- Multiple performance improvements #573
- Fixes a bug where path generation would deduplicate segments leading to the wrong path #573
- Fixes a bug where the CodeDOM would be corrupted (bad tree) leading to incoherent generation results #573
- Fixes a bug where the generator would duplicate some models #573
- Moves the models to a dedicated namespace (models) #573
- Fixes a bug where enum serialization would be calling the wrong method in TypeScript #573
- Fixes a bug where request body would use the response schema #573
- Fixes an issue where type could conflict with namespace names and prevent CSharp compilation #573
- Fixes an issue where primitive types would map to the wrong serialization method in dotnet #573
- Fixes an issue where union models would not be able to deserialize because of missing members #573
- Fixes an issue where request builder methods would refer to unexisting properties in dotnet #573
- Fixes an issue where duplicated symbols for different imports would make java compilation fail #573
- Adds missing type mappings in java #573
- Fixes an issue where Go generation could use reserved keywords #573
- Fixes a bug where Go generation could end up with circular dependencies in models #573
- Fixes a bug where Go generation would map the wrong http core method for primitive types #573
- Fixes a bug where Go generation would have unused imports making build fail #573
- Fixes a bug where missing type definitions would make Ruby generation fail #573
- Fixes a bug where Go generation would miss the module symbol for inherited constructors #573

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
