# AL Language Support in Kiota

This directory contains the implementation for generating AL (Application Language) code using Kiota. The changes in this fork introduce refinements and writers specific to AL, enabling the generation of AL-compatible client libraries.

## Key Changes

### Refinements (`src/Kiota.Builder/Refiners/ALRefiner.cs`)

The [`ALRefiner`](src/Kiota.Builder/Refiners/ALRefiner.cs) class customizes the code generation process for AL. Key updates include:

- **Property Handling**: Since AL does not support properties, they are converted into getter and setter methods.
- **Method Name Adjustments**: Removes `-overload` suffixes from method names to ensure compatibility with AL naming conventions.
- **Parameter Updates**: Reserved parameter names are prefixed with underscores to avoid conflicts.
- **Additional Data Removal**: The [`AdditionalData`](src/Kiota.Builder/CodeDOM/CodeProperty.cs) property is removed as it is not supported in AL.
- **Request Builder Updates**: Adds methods and variables to handle request configurations and indexers in AL.
- **Model Class Adjustments**: Removes inheritance and adds default implementations for model classes.
- **Request Executor Updates**: Adds custom parameters and configurations for request execution methods.

### Writers (`src/Kiota.Builder/Writers/AL`)

The writers in this directory handle the generation of AL-specific code elements:

- **[`ALWriter`](src/Kiota.Builder/Writers/AL/ALWriter.cs)**: Manages the overall writing process for AL code, including path segmentation and conventions.
- **`CodeClassWriter`**: Handles the generation of AL classes.
- **[`CodeMethodWriter`](src/Kiota.Builder/Writers/AL/CodeMethodWriter.cs)**: Writes methods, including custom handling for AL-specific conventions.
- **[`CodePropertyWriter`](src/Kiota.Builder/Writers/AL/CodePropertyWriter.cs)**: Converts properties into methods for AL compatibility.
- **[`CodeIndexerWriter`](src/Kiota.Builder/Writers/AL/CodeIndexerWriter.cs)**: Supports indexer generation in AL.
- **[`ALConventionService`](src/Kiota.Builder/Writers/AL/ALConventionService.cs)**: Provides naming and formatting conventions specific to AL.

## Custom Handling for Additional Information

To avoid modifying the existing Kiota base, a custom mechanism has been introduced in the `CustomPropertyExtension`. This extension internally uses a documentation label to store additional information required during the code generation process. These labels act as placeholders for metadata or configuration details that are not directly supported by the base Kiota implementation.

Before the final documentation is written to the generated code, these labels are removed to ensure they do not appear in the output. This approach maintains compatibility with the Kiota base while enabling the flexibility needed for AL-specific requirements.

## Features

- **AL-Specific Conventions**: Ensures generated code adheres to AL syntax and conventions.
- **Customizable Configuration**: Supports client namespace and base URL configuration through [`ALConfigurationHelper`](src/Kiota.Builder/Refiners/ALConfigurationHelper.cs).
- **Enhanced Compatibility**: Handles reserved names, unsupported features, and AL-specific requirements.

## Usage

To generate AL code using Kiota, ensure the following:

1. Configure the [`GenerationConfiguration`](src/Kiota.Builder/Configuration/GenerationConfiguration.cs) to specify AL as the target language.
2. Run the Kiota generator with the appropriate input specifications.
3. The generated code will adhere to AL conventions and be ready for use in AL projects.

## Disclaimer

This is basically a draft and I'm not sure if this is up to best practices. This should be used a starting point for discussions within the community.
