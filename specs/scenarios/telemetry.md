# Telemetry

Kiota is a tool that generates code for interacting with an API. It is used by developers and API owners to create code clients that interact with APIs. And with the new commands that generate manifests, it will also be used for developers to create AI manifests. In both cases, it is important to understand how the tool is being used and what are the scenarios leveraged by our community. This document describes the telemetry we plan to collect and how we use it.

## Current Challenges

- Kiota doesn't have a telemetry component as part of its CLI experience.
- The Kiota team doesn't have visibility on how the tool is being used and what are the scenarios that are important to our community.
- When planning for future investments, the Kiota team doesn't have a way to prioritize scenarios based on their importance to our community.

## Goals

- Understand how Kiota is being used.
- Understand what scenarios are important to our community.
- Understand what are the preferred experiences for using Kiota.

## Non-goals

- We are not trying to collect any personally identifiable information.
- We are not trying to collect any information about the API being used.
- We are not trying to collect any information about the application using the API.
- We are not trying to collect any information during the runtime of the application.

## Proposal

We should introduce a new telemetry component to Kiota. This component should be enabled by default and should be able to be disabled by the user, in a very similar way that the `dotnet` CLI does (https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry). The telemetry component should be able to collect information about the following scenarios:

- Adoption of the CLI in general.
- Adoption of the different commands.
- Adoption of the different parameters for each command (without values that could be considered sensitive like the OpenAPI description, include / exclude paths, etc.).
- Adoption of the different languages.
- Adoption of the different platforms we offer support for.

### Privacy

We should be very careful about the information we collect. Before rolling out this feature, we should have full agreement with CELA on our approach, the way we collect and protect the data. In general:

- We should offer a way to opt-out of the telemetry collection.
- We should not collect any information that could be considered sensitive. 
- We should not collect any information that could be used to identify a person, an application, or an API. 
- We should not collect any information during the runtime of the application.

### Examples of questions we want to answer

- How many users are using the CLI?
- How many users are using the CLI via the extension?
- What is the current growth trajectory of the usage of kiota?
- What are the most used commands?
- How are the commands used? Which parameters are being used?
- What are the most used languages?
- Are users levaring Kiota for AI?
- What are the most used type of manifest?
- What are the most used platforms?
- Is there a difference in how the CLI is being used between platforms?
- Do we have users using old versions of kiota? Why?
- When launching new capabilities, what is the adoption rate? How long does it take for users to adopt the new capabilities?
- Can we identify a spike in error being returned by Kiota? What is the impact of the error? What command generates the error? What parameters are being used?

### Data collected

#### Basic data being collected

For every command, we should collect the following information:

- Timestamp
- Operating system
- Operating system version
- Source (CLI or extension)
- Acquisition channel (dotnet tool, binaries, homebrew, asdf, extension, etc.)
- Kiota version
- VS Code extension version (if applicable)
- Command name
- Command parameters being used (without their values)
- Command execution time
- Command result (success or failure)

#### Command-specific data being collected

Every command has a different set of parameters. We should collect relevant parameters (and their values) for each command. The data collected shouldn't include any information that could be considered sensitive, only system-related information. Each command specification should include the list of parameters that should be collected and whether their values should be collected or not.

The list of commands and their parameters can be found in the [CLI Commands](../cli/index.md) section. Each parameter indicates whether its value should be collected or not.

### Opting-out

We should offer a way to opt-out of the telemetry collection. This should be done in a very similar way that the `dotnet` CLI does (https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry). To opt out of the telemetry feature, set the KIOTA_CLI_TELEMETRY_OPTOUT environment variable to 1 or true.

Every time the CLI is installed and updated, we should inform the user about the telemetry feature and how to opt-out of it. If the users already opted-out, we should not inform the user and respect their choice.

```bash
Telemetry
---------
Kiota collect usage data in order to help us improve your experience. You can opt-out of telemetry by setting the KIOTA_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.

Read more about Kiota telemetry: https://aka.ms/kiota/docs/telemetry
```