# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

## [0.6.0] - 2022-10-06

### Added

- Added a search command to find APIs.
- Added a download command to download API descriptions.
- Added a show command to display the API paths as a tree.
- Added an info command to show languages maturity and dependencies.
- Added hints to help people use and discover the commands.
- Added arguments to filter path items during generation (include-path/exclude-path).
- Added the ability to cancel the refinement process.
- Added Java 8 generation support.
- Added tracing support for Go. [#618](https://github.com/microsoft/kiota/issues/618)

### Changed

- BREAKING: the generation command is now a sub command: `kiota generate ...` instead of `kiota ...`.
- Fixed a bug where OData primitive types would result in composed types.
- Fixed a concurrency issue with imports management.
- Fixed a bug where Java request options type could conflict with generated types.
- Fixed a bug where CSharp serialization/deserialization names for properties would always be lowercased. [#1830](https://github.com/microsoft/kiota/issues/1830)
- Fixed a regression where the incorrect schema would be selected in an AllOf collection to generate incorrect type inheritance.
- Fixed a bug where discriminator information could contain non-derived types. [#1833](https://github.com/microsoft/kiota/issues/1833)
- Fixes a bug where mapping value would be missing from factories. [#1833](https://github.com/microsoft/kiota/issues/1833)
- Update go serializers and deserializers to use abstractions utils

## [0.5.1] - 2022-09-09

### Added

- Exempts read only properties from being serialized and sent to the service. [#1828](https://github.com/microsoft/kiota/issues/1828)

### Changed

- Fixed a regression where parse node parameter type for factories would be incorrect in Go, Ruby, Swift, Java and TypeScript.

## [0.5.0] - 2022-09-08

### Added

- Added support for range (2XX) responses. [#1699](https://github.com/microsoft/kiota/issues/1699)
- Added none output formatter to CLI commons. (Shell)
- Added 'Accept' field of http request header in Ruby.  [#1660](https://github.com/microsoft/kiota/issues/1660)
- Added support for text serialization in Python. [#1406](https://github.com/microsoft/kiota/issues/1406)
- Added support for composed types (union, intersection) in CSharp, Java and Go. [#1411](https://github.com/microsoft/kiota/issues/1411)
- Added support for implicit discriminator mapping.
- Added support for default values of enum properties in CSharp, Java and Go.

### Changed

- Fixed a bug where Go clients would panic in case of nil response value.
- Fixed a bug to properly add request headers to Nethttp requests in Ruby. 
- Fixed a bug to properly reject invalid URLs in Ruby.
- Fixed an issue with require statements being generated instead of require relative in Ruby. 
- Updated AdditionDataHolder with the correct namespace. (Ruby)
- Removed/fixed passing in the current instance to fields deserializers in Ruby. [#1663](https://github.com/microsoft/kiota/issues/1663)
- Fix issue with duplicate variable declaration in command handlers (Shell)
- Update namespace qualification algorithm (helps in resolving when a type name appears in multiple namespaces) to use case insensitive string comparison (CSharp).
- Fix an issue where namespace reserved name replacement would not include replacing import names in the declared areas in CSharp. [#1799](https://github.com/microsoft/kiota/issues/1799)
- Removed Python abstractions, http, authentication and serialization packages
- Fixed an issue with generating the incorrect serialized type name and require statement for get/post methods (Ruby).
- Remove all overloads for GO request executors
- Adds a context object in all GO requests
- Remove all overloads for GO request executors and Adds a context object in all GO requests [GO#176](https://github.com/microsoftgraph/msgraph-sdk-go/issues/176)
- Fixed a bug where the Hashing method for type names differentiation could lock the process.
- Fixed a bug where CSharp declaration writer would add usings for inner classes.
- Fixed a bug with inline schema class naming.
- Fixed a bug where symbols starting with a number would be invalid.
- Fixed a bug where classes could end up with duplicated methods.
- Fixed a bug where Go writer would try to import multiple times the same symbol.
- Fixed a bug where the core generator engine would fail to recognize meaningful schemas.
- Fixed a bug where Go and Java inner class imports would be missing.
- Fixed a bug where Go and Java collection bodies would not generate properly.
- Aligns request options types in Java with other collections type.
- Fixed a bug where Java would skip duplicated imports instead of deduplicating them.
- Fixed a bug where Java would not convert date types for query parameters.
- Fixed a bug where Java doc comments could contain invalid characters.
- Fixed a bug where function parameters would be reodered incorrectly in dotnet[#1822](https://github.com/microsoft/kiota/issues/1822)

## [0.4.0] - 2022-08-18

### Added

- Updated test suite and tooling for python abstractions and core packages. [#1761](https://github.com/microsoft/kiota/issues/367)
- Added support for no-content responses in python abstractions and http packages. [#1630](https://github.com/microsoft/kiota/issues/1459)
- Added support for vendor-specific content types in python. [#1631](https://github.com/microsoft/kiota/issues/1463)
- Simplified field deserializers for json in Python. [#1632](https://github.com/microsoft/kiota/issues/1492)
- Adds python code generation support. [#1200](https://github.com/microsoft/kiota/issues/163)
- Added native type support for Duration, Time Only, and Date Only in Ruby. [#1644](https://github.com/microsoft/kiota/issues/1644)
- Added a `--additional-data` argument to generate the AdditionalData properties [#1772](https://github.com/microsoft/kiota/issues/1772)
- Added CAE infrastructure in Ruby by adding an `--additional-properties` parameter to the authenticate method of AuthenticationProvider, the get access token method of the AccessTokenProvider in Ruby. [#1643](https://github.com/microsoft/kiota/issues/1643)
- Added Kiota authentication library for Ruby. [#421](https://github.com/microsoft/kiota/issues/421)

### Changed

- Fixed a bug where collections types would generate invalid return types in CSharp.
- Fixed a bug where a nullable entry in anyOf schemas would create unnecessary composed types.
- Removed duplicate properties defined in base types from model serialization and deserialization methods and initialise property defaults in constructor. [#1737](https://github.com/microsoft/kiota/pull/1737)
- Fixed a bug where the generated code had incorrect casing within a method (Ruby). [#1672](https://github.com/microsoft/kiota/issues/1672)
- Fixed an issue where duplicate 'require' statements are generated for inner classes in the middle of the file (Ruby). [#1649](https://github.com/microsoft/kiota/issues/1649)
- Split parsable interface and additional property/data interface in Ruby. [#1654](https://github.com/microsoft/kiota/issues/1654)
- Changed format of datetimes in Go to be converted to ISO 8601 by default when place in path parameters(Go)
- Defined the Access Token Provider Interface for Ruby authentication. [#1638](https://github.com/microsoft/kiota/issues/1638)
- Reduce code verbosity on Go Getters and Setters. [G0#26][https://github.com/microsoftgraph/msgraph-sdk-go-core/issues/26]


## [0.3.0] - 2022-07-08

### Added

- Added a more explicit error message for invalid schemas. [#1718](https://github.com/microsoft/kiota/issues/1718)
- Added a parameter to specify why mime types to evaluate for models. [#134](https://github.com/microsoft/kiota/issues/134)
- Added an explicit error message for external references in the schema. [#1580](https://github.com/microsoft/kiota/issues/1580)
- Added accept header for all schematized requests. [#1607](https://github.com/microsoft/kiota/issues/1607)
- Added support for paging. [#1569](https://github.com/microsoft/kiota/issues/1569)
- Added support for vendor specific content types(PHP) [#1464](https://github.com/microsoft/kiota/issues/1464)
- Added support for accept request header (PHP) [#1616](https://github.com/microsoft/kiota/issues/1616)
- Added Getting Started steps for PHP. [#1642](https://github.com/microsoft/kiota/pull/1642)
- Defined the Access Token Provider interface (Ruby) [#1638](https://github.com/microsoft/kiota/issues/1638)
- Added Continuous Access Evalution infrastructure (Ruby) [#1643](https://github.com/microsoft/kiota/issues/1643)

### Changed

- Fixed a bug where query parameter types would not consider the format. [#1721](https://github.com/microsoft/kiota/issues/1721)
- Fixed a bug where discriminator mappings across namespaces could create circular dependencies in Go. [#1712](https://github.com/microsoft/kiota/issues/1712)
- Fixed a bug where Go binary downloads would try to parse a structured object.
- Aligned mime types model generation behaviour for request bodies on response content. [#134](https://github.com/microsoft/kiota/issues/134)
- Fixed an issue where some critical errors would not return a failed exit code. [#1605](https://github.com/microsoft/kiota/issues/1605)
- Moved nested request configuration classes into separate files within the namespace for PHP. [#1620](https://github.com/microsoft/kiota/pull/1620)
- Fixed an issue where duplicate 'require' statements are generated for inner classes in the middle of the file (Ruby). [#1649](https://github.com/microsoft/kiota/issues/1649)
- Fixed wrong parameter type for Request config for request executors(PHP). [#1629](https://github.com/microsoft/kiota/pull/1629)
- Increased indentation for errorMappings in the request executor (PHP). [#1629](https://github.com/microsoft/kiota/pull/1629)
- Fixed bugs in PHP discriminator factory methods, Guzzle request adapter send methods, stream and plain text response handling. [#1634](https://github.com/microsoft/kiota/pull/1634)
- Removed abstractions, authentication, http and serialization packages for PHP. [#1637](https://github.com/microsoft/kiota/pull/1637)
- Fixed a bug where generated discriminator methods would reference types in other namespaces without proper resolution. [#1670](https://github.com/microsoft/kiota/issues/1670)
- Fixed a bug where additional data and backing store properties would be duplicated. [#1671](https://github.com/microsoft/kiota/issues/1671)
- Fixed a bug where serialized properties would not match the json property name when using the backing store. (CSharp).
- Corrected PHPDoc types for headers and request options properties in request configuration classes. [#1711](https://github.com/microsoft/kiota/pull/1711)
- Fixed a bug where properties defined at multiple inherited models would collide. [#1717](https://github.com/microsoft/kiota/issues/1717)

## [0.2.1] - 2022-05-30

### Added

- Added missing mappings in PHP for uint8 and int8. [#1473](https://github.com/microsoft/kiota/pull/1473)
- Added support for enum and enum collections responses in Go. [#1578](https://github.com/microsoft/kiota/issues/1578)
- Added Kiota builder engine as a package for external services integration. [#1582](https://github.com/microsoft/kiota/issues/1582)

### Changed

- Fixed a bug where the logger would not log all the information. [#1588](https://github.com/microsoft/kiota/issues/1588)

## [0.2.0] - 2022-05-24

### Added

- Added support for enum options descriptions (C#/Go/Java/TypeScript). [#90](https://github.com/microsoft/kiota/issues/90)
- Added support for file parameters types. [#221](https://github.com/microsoft/kiota/issues/221)
- Added support for no content responses in PHP. [#1458](https://github.com/microsoft/kiota/issues/1458)
- Added support for error handling in php request adapter. [#1157](https://github.com/microsoft/kiota/issues/1157)
- Added support for discriminator downcast in PHP. [#1255](https://github.com/microsoft/kiota/issues/1255)
- Added support for multiple collections indexing under the same parent.
- Added code exclusions placeholder in the generation. (oneOf)
- Added support for continuous access evaluation in Java. [#1179](https://github.com/microsoft/kiota/issues/1179)
- Added support for special characters in URL query parameter names. [#1584](https://github.com/microsoft/kiota/pull/1584)

### Changed

- Fixed a bug where union types would not work as error types.
- Fixed a bug where generation names could collide with platform names in CSharp.
- Fixed missing numbers mapping cases.
- Fixed multiple bugs enum options invalid symbols generation.
- Fixed a bug where symbols (classes, enums, properties...) could be only numbers, which is unsupported by most languages.
- Fixed a bug where union types would be missing serialization information.
- Fixed a bug where inline request bodies could override each other for the same path item with multiple operations.
- Fixed simple collections (arrays) support in CSharp.
- Fixed a bug where code properties could not be union or exclusion types.
- Fixed a bug where models would fail to generate if the schema type wasn't set to object.
- Fixed a bug where nullable wrapper schema flattening would ignore some composed type options.
- Fixed a bug where arrays without items definition would derail generation.
- Fixed a bug with enums detection for generation. (interpreted as string)
- Fixed a bug where classes names cleanup could end-up in a collision.
- Fixed a bug where null reference exception would be thrown when trying to lookup type inheritance on discriminators
- Fixed the lookup of model namespaces to only look in the target namespace to avoid reference collisions.
- Fixed a bug for the generated send method for paths returning Enums in dotnet.
- Breaking: renamed the --loglevel parameter to --log-level.
- Fixed a bug where some path parameter objects would have empty key values [#1586](https://github.com/microsoft/kiota/issues/1586)

## [0.1.3] - 2022-05-06

### Added

- Added text serialization library for PHP. [#1546](https://github.com/microsoft/kiota/pull/1546).

### Changed

- Fixed the image name in CI for MCR.

### Changed

## [0.1.2] - 2022-05-06

### Changed

- Minor changes in the parameters (-co => --co, -ll => --ll, -d is required, -l is required).

## [0.1.1] - 2022-05-06

### Changed

- Add binder for nullable boolean options. (Shell)

## [0.1.0] - 2022-05-04

### Added

- The dotnet tool is now available on the public feed `dotnet tool install -g Microsoft.OpenApi.Kiota --prerelease`.
- The dotnet OpenApi reference package is now available `Microsoft.OpenApi.Kiota.ApiDescription.Client`.
- The container image is now available on mcr. `docker pull mcr.microsoft.com/kiota/generator:latest`.

### Changed

- Revamped the api surface for request configuration. [#1494](https://github.com/microsoft/kiota/issues/1494)
- Fixed a bug in methods naming in Go after request configuration revamp.
- Fixes a bug where reserved names would not be updated for inheritance.
- Add `item` subcommand for indexers. Fixes conflicts when paths have repeating segments. (Shell) [#1541](https://github.com/microsoft/kiota/issues/1541)

## [0.0.23] - 2022-04-19

### Changed

- Fixed a bug where line returns in descriptions could break the generated code. [#1504](https://github.com/microsoft/kiota/issues/1504)
- Fixed a bug with special characters in query parameters names. [#1445](https://github.com/microsoft/kiota/issues/1445)
- Fixed a bug where complex types path parameters would fail to generate.
- Fixed a bug where Go serialization/deserialization method would generate invalid accessor names.
- Added discriminator support in the python abstractions serialization and http packages. [#1500](https://github.com/microsoft/kiota/issues/1256)

## [0.0.22] - 2022-04-08

### Added

- Added generation of command options for headers defined in the OpenAPI metadata source file. (Shell)
- Added retry, redirect, chaos and telemetry handler in java.

### Changed

- Simplified field deserialization.(PHP) [#1493](https://github.com/microsoft/kiota/issues/1493)
- Fixed a bug where the generator would not strip the common namespace component id for models. [#1483](https://github.com/microsoft/kiota/issues/1483)
- Simplified field deserialization. [#1490](https://github.com/microsoft/kiota/issues/1490)

## [0.0.21] - 2022-04-01

### Added

- Added text output formatter to CLI commons. (Shell)
- Added support for vendor specific content types generation/serialization. [#1197](https://github.com/microsoft/kiota/issues/1197)
- Added support for 204 no content in generation and CSharp/Java/Go/TypeScript request adapters. #1410
- Added a draft swift generation implementation. #1444
- Added support for yaml response type generation. [#302](https://github.com/microsoft/kiota/issues/302)
- Added support for xml response type generation. [#302](https://github.com/microsoft/kiota/issues/302)
- Added support for unstructured response generation (stream). [#546](https://github.com/microsoft/kiota/issues/546)

### Changed

- Moved go libraries to their own repository. [#370](https://github.com/microsoft/kiota/issues/370)
- Fixed a bug where the base url of the request adapter would be reset by the client(PHP). [#1469](https://github.com/microsoft/kiota/issues/1469)
- Fixed issue where custom date types are never corrected for method parameters(PHP). #1474
- Replaced DateTimeOffset with DateTime for custom date types(PHP). #1474
- Fixed a bug where the base url of the request adapter would be reset by the client. [#1443](https://github.com/microsoft/kiota/issues/1443)
- Fixed a bug where request builder classes for collections endpoints would have a wrong name. #1052
- Fixed issue with ambiguous type names causing build errors and stack overflows. (Shell) #1052
- Fixed a bug where symbols (properties, methods, classes) could contain invalid characters #1436
- Renamed parameters for requests: o => options, q => queryParameters, h => headers. [#1380](https://github.com/microsoft/kiota/issues/1380)
- Fixed a bug where names would clash with reserved type [#1437](https://github.com/microsoft/kiota/issues/1437)
- Fixed unnecessary use of fully qualified type names in Dotnet.

## [0.0.20] - 2022-03-25

### Changed

- Moved TypeScript middleware from Graph core to kiota http.
- Fixed a bug where errors would fail to deserialize for TypeScript.
- Fixed a bug where decimal types would not be mapped in TypeScript.
- Fixed circular dependencies issues for TypeScript #870.
- Fixed a bug where JSON serialization would fail on nil properties in Go.
- Moved typescript core packages into Kiota-TypeScript repo and delete for Kiota repo.
- Fixed a bug where collections of complex types could be mis-represented. [#1438](https://github.com/microsoft/kiota/issues/1438)
- Fixed a bug where inline properties would not generate their own type definition. [#1438](https://github.com/microsoft/kiota/issues/1438)

## [0.0.19] - 2022-03-18

### Added

- Adds a `--clean-output` argument to clean the target directory before generation #1357
- Adds support for `text/plain` responses for CSharp, Java, TypeScript and Go. #878

### Changed

- Fixed a bug where models descriptions would not be deterministic #1393
- Fixed a bug where unnecessary namespaces would be added to models generation #1273
- Fixed a bug where Go byte arrays would not write deserializers properly.
- Fixed a bug where integers would not be recognized when type is not number.
- Fixed a bug where union types with primitive member types would fail to generate #1270
- Fixed a bug where union types with inline schema member types would fail to generate #1270
- Fixed a bug where referenced types with no titles would fail to generate #1271
- Fixed a bug where the generator would introduce unnecessary union types for nullables. #990
- Moved all the dotnet libraries to their own repository. #1409

## [0.0.18] - 2022-03-14

### Added

- Added default implementations for table and JSON output in CLI commons (Shell) #1326
- Adds missing mapped types (int8, uint8, commonmark, html, ...) #1287

### Changed

- Add missing method getBinaryContent to the ParseNode interface(PHP).
- Split the Parsable interface into AdditionalData interface and Parsable interface(PHP) #1324.
- Shell commands will now default to writing indented JSON. This option can be disabled through the CLI option `--json-no-indent` (Shell) #1326
- Update System.CommandLine version (Shell) #1338
- Add async writers in output formatters (Shell) #1326
- Add async filter function in output filters (Shell) #1326
- BREAKING: Remove synchronous version of WriteOutput that accepts a stream input (Shell) #1326
- BREAKING: Remove synchronous version of WriteOutput that accepts a string input (Shell) #1326
- BREAKING: Remove synchronous version of FilterOutput that accepts a string input (Shell) #1326
- Fixed a bug where error responses without schema would make generation fail #1272
- Fixed indeterministic parameters ordering #1358
- Fixed indeterministic error mappings ordering #1358
- Fixed indeterministic discriminator mapping ordering #1358
- Fixed race condition when removing child items leading to erratic code generation results #1358
- Replaced models namespaces flattening by circular properties trimming in Go #1358
- Fixed a bug where inherited interfaces would be missing imports in Go #1358
- Fixed a bug where inherited interfaces would be missing imports for the parent #1358
- Fixed bugs across request adapter and serialization in PHP #1353
- Fixed NullReferenceException in Go generator
- Fixed incorrect mapping when the response type is `text/plain` #1356
- Fixed a bug in Dotnet.Typescript where properties could have invalid characters #1354
- Improved error display #1269
- Fixed a bug where union wrapper models would lack the discriminator methods.
- Fixed bug working with async azure credentials in Python.
- Fixed minor issues around PHP Generation, Serialization and Abstractions.
- Fix Discriminator support for PHP.
- Move additional data from Parsable into AdditionalDataHolder base class in Python #1360

## [0.0.17] - 2022-03-03

### Added

- Adds support for downcast of types during deserialization according to the discriminator information in the description (CSharp/Go/Java/TypeScript). [#646](https://github.com/microsoft/kiota/issues/646)
- Adds support for generating interfaces for models in Go. [#646](https://github.com/microsoft/kiota/issues/646)
- Adds support for generating functions (as opposed to methods or static methods) in the generator (used in TypeScript for discriminator factories). [#646](https://github.com/microsoft/kiota/issues/646)
- Added support for global error handling in python abstractions #1289
- Added a HTTPRequestAdapter for python Requests library #1251
- Added Shell output filter (JMESPath) support #1291
- Added output options to Shell output filter #1321

### Changed

- Fixed a bug in Go generator where temporary url template parameters would not be used preventing the use of raw urls.
- Fixed a bug where the Go http client configuration would impact non-kiota requests.
- Fixed bug where installing python abstractions failed due to missing dependencies  #1289
- Modified python test matrix to include python 3.10  #1289
- Added return statement to AnonymousAuthenticationProvider in python abstractions  #1289
- Fixed bug in enabling backing store for parse node factory by passing ParseNodeFactoryRegistry to method call  #1289
- Fixed errors in python serialization due to to responses as json instead of json strings #1290
- Added python version 3.10 to testing matrix #1290 
- Fixed bug with inconsistent Java namespace and directory name casing #1267
- Fixed typeOf string check in JsonParseNode Typescript.
- Fixed shell stream output getting processed by output formatters when no file path is provided #1291
- Using Record type instead of Map for additionalData in TypeScript

## [0.0.16] - 2022-02-23

### Added

- Added the ability to configure the underlying transport in Go. #1003
- Added additional date time (date, time, duration) types in the generation process. #1017
- PHP Request Adapter (includes middleware) #1048, #918, #1024, #1025
- Added support for PHP Json Serialization.
- Adds Python abstractions library. #925
- Adds hostname and protocol validation in authentication. #1051
- Adds Azure Identity Authentication Provider for Python. #1108
- Adds JSON Serialization library for Python. #1186
- Adds PHP League Authentication Provider for PHP #1201
- Added Shell language support #738


### Changed

- Fixed a bug where request body would get dropped by the compression handler in Go
- Fixed an issue where multiple api clients could run into racing conditions in Go.
- Fixed a bug where empty additional data in Go would lead to invalid JSON payloads during serialization.
- Fixed a bug where Go serialization would write empty arrays for nil values.
- Modified the TypeScript RequestInformation URL paramater data type from URL to string.
- Modified TypeScript packages to provide CJS and ESM modules.
- Modified the TypeScript RequestInformation query and path paramaters data type from Map to Record Type.
- Modified TypeScript RequestInformation headers and options to Record type.
- Modified the TypeScript RequestInformation content data type to ArrayBuffer.
- Updated PHP abstractions to make property keys and values nullable in `SerializationWriter.php`.
- Fixed an issue where enum collections parsing would fail in Go.
- Breaking. Kiota clients generate error types and throw when the target API returns a failed response (dotnet, go, java, typescript). #1100
- Fixed missing methods for serializing/deserializing decimal values in dotnet #1252
- Modified RequestBuilder types are suffixed with the ItemRequestBuilder if they belong to an item namespace to fix name collisions #1252
- Modified the use of fully qualified name of types in dotnet to ensure the target type and current element are not in the same namespace #1252.

## [0.0.15] - 2021-12-17

### Changed

- Fixes name collisions in dotnet by renaming "HttpMethod" enum to "Method" in dotnet abstractions
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
- Fixes a bug where generation would fail because of empty usings.
- Fixes a bug where Java and Go escaped model properties would not serialize properly.
- Fixes a bug where null values would not be added to additionalData if there was no matching property in dotnet.
- Fixes a bug where deserialzation of enums would throw an ArgumentExcpetion if the member didn't exist in dotnet.

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
