---
parent: Welcome to Kiota
nav_order: 4
---

# Releases and support for Kiota

Microsoft Graph ships major releases, minor releases and patches for [Kiota](index.md) and the [Microsoft Graph SDKs](https://docs.microsoft.com/graph/sdks/sdks-overview). This article explains release types and support options for Kiota.

## Release types

Kiota follows [Semantic Versioning 2.0.0](https://semver.org/). Information about the type of each release is encoded in the version number in the form `major.minor.patch`.

### Major releases

Major releases include new features, new public API surface area, and bug fixes. Due to the nature of the changes, these releases are expected to have breaking changes. Major releases can install side by side with previous major releases. We will only maintain the latest major release, except for security and privacy related issues where we may release patches to prior major versions. Major releases are published as needed.

### Minor releases

Minor releases also include new features, public API surface area, and bug fixes. The difference between these and major releases is that minor releases will not contain breaking changes to the tooling experience or the generated API surface area of stable maturity languages. Minor releases can install side by side with previous minor releases. Minor releases are targeted to publish on the first Tuesday of every month.

### Patches

Patches ship as needed, and they include security and non-security bug fixes.

## Language support

Language support in Kiota is either stable, preview or experimental for each language.

The following criteria is used to determine the maturity levels.

- **Stable**: Kiota provides full functionality for the language and has been used to generate production API clients.
- **Preview**: Kiota is at or near the level of table but hasn't been used to generate production API clients.
- **Experimental**: Kiota provides some functionality for the language but is still in early stages. Some features may not work correctly or at all.

Breaking changes to languages that are stable will result in major version change for Kiota tooling. Code generation changes to a stable maturity language are not considered breaking when they rely on additions in the corresponding abstractions library as these changes will only require to update the abstractions library the the latest minor/patch version under the same major version.

The current status of language support can be queried by using the following command.

```bash
kiota info
```

## Kiota roll-forward and compatibility

Major and minor updates can be installed side by side with previous versions. Installing new versions of the Kiota tooling does not impact existing Kiota generated code. To update applications to the latest Kiota generate code, the code must be regenerated with the new Kiota tooling. The app doesn't automatically roll forward to use a newer version of Kiota Tooling.

We recommend rebuilding the app and testing against a newer major or minor version before deploying to production.

## Support

The primary support channel for Kiota is GitHub. You can open GitHub issues in the [Kiota repository](https://github.com/microsoft/kiota) to track bugs and feature requests.
