# @microsoft/kiota

This library provides various functions to interact with Kiota, a client generator for HTTP REST APIs described by OpenAPI.

## Installation

To install the package, use the following command:

```bash
npm install @microsoft/kiota
```

## Usage

Once installed, you can use the available functions to generate and interact with Kiota clients.
Below is a reference for each function, including parameters and return values.

### `generateClient`

Generates a client based on the provided client generation options.

**Parameters:**

- `clientGenerationOptions`: *ClientGenerationOptions* - The options for generating the client.
* Options for generating a client.
- `clientGenerationOptions.openAPIFilePath`: *string* - The file path to the OpenAPI specification.
- `clientGenerationOptions.clientClassName`: *string* - The name of the client class to generate.
- `clientGenerationOptions.clientNamespaceName`: *string* - The namespace name for the generated client.
- `clientGenerationOptions.language`: *KiotaGenerationLanguage* - The programming language for the generated client.
- `clientGenerationOptions.outputPath`: *string* - The output path for the generated client.
- `clientGenerationOptions.operation`: *ConsumerOperation* - The consumer operation to perform.
- `clientGenerationOptions.workingDirectory`: *string* - The working directory for the generation process.

- `clientGenerationOptions.clearCache` (optional): *boolean* - Whether to clear the cache before generating the client.
- `clientGenerationOptions.cleanOutput` (optional): *boolean* - Whether to clean the output directory before generating the client.
- `clientGenerationOptions.deserializers` (optional): *string[]* - The list of deserializers to use.
- `clientGenerationOptions.disabledValidationRules` (optional): *string[]* - The list of validation rules to disable.
- `clientGenerationOptions.excludeBackwardCompatible` (optional): *boolean* - Whether to exclude backward-compatible changes.
- `clientGenerationOptions.excludePatterns` (optional): *string[]* - The list of patterns to exclude from generation.
- `clientGenerationOptions.includeAdditionalData` (optional): *boolean* - Whether to include additional data in the generated client.
- `clientGenerationOptions.includePatterns` (optional): *string[]* - The list of patterns to include in generation.
- `clientGenerationOptions.serializers` (optional): *string[]* - The list of serializers to use.
- `clientGenerationOptions.structuredMimeTypes` (optional): *string[]* - The list of structured MIME types to support.
- `clientGenerationOptions.usesBackingStore` (optional): *boolean* - Whether the generated client uses a backing store.

**Returns:** A promise that resolves to a KiotaResult if successful, or undefined if not.

**Throws:**
- If an error occurs during the client generation process.

### `generatePlugin`

Generates a plugin based on the provided options.

**Parameters:**

- `pluginGenerationOptions`: *PluginGenerationOptions* - The options for generating the plugin.
- `pluginGenerationOptions.openAPIFilePath`: *string* - The file path to the OpenAPI specification.
- `pluginGenerationOptions.pluginName`: *string* - The name of the plugin to generate.
- `pluginGenerationOptions.outputPath`: *string* - The output path where the generated plugin will be saved.
- `pluginGenerationOptions.operation`: *ConsumerOperation* - The operation to perform during generation.
- `pluginGenerationOptions.workingDirectory`: *string* - The working directory for the generation process.

- `pluginGenerationOptions.pluginType` (optional): *KiotaPluginType* - The type of the plugin to generate.
- `pluginGenerationOptions.includePatterns` (optional): *string[]* - The patterns to include in the generation process.
- `pluginGenerationOptions.excludePatterns` (optional): *string[]* - The patterns to exclude from the generation process.
- `pluginGenerationOptions.clearCache` (optional): *boolean* - Whether to clear the cache before generation.
- `pluginGenerationOptions.cleanOutput` (optional): *boolean* - Whether to clean the output directory before generation.
- `pluginGenerationOptions.disabledValidationRules` (optional): *string[]* - The validation rules to disable during generation.
- `pluginGenerationOptions.pluginAuthType` (optional): *PluginAuthType | null* - The authentication type for the plugin, if any.
- `pluginGenerationOptions.pluginAuthRefid` (optional): *string* - The reference ID for the plugin authentication, if any.

**Returns:** A promise that resolves to a KiotaResult if successful, or undefined if not.

**Throws:**
- If an error occurs during the generation process.

The function connects to Kiota and sends a request to generate a plugin using the provided options.
It handles the response and checks for success, returning the result or throwing an error if one occurs.

### `getKiotaTree`

Shows the Kiota result based on the provided options.

**Parameters:**

- `options`: *KiotaResultOptions* - The options to configure the Kiota result.
- `options.descriptionPath`: *string* - The path to the description file.

- `options.includeFilters` (optional): *string[]* - Filters to include in the result.
- `options.excludeFilters` (optional): *string[]* - Filters to exclude from the result.
- `options.clearCache` (optional): *boolean* - Whether to clear the cache before showing the result.
- `options.includeKiotaValidationRules` (optional): *boolean* - Whether to evaluate built-in kiota rules when parsing the description file.

**Returns:** A promise that resolves to the Kiota show result or undefined if an error occurs.

**Throws:**
- Throws an error if the result is an instance of Error.

### `getKiotaVersion`

Retrieves the version of Kiota by connecting to the Kiota service.

**Returns:** A promise that resolves to the Kiota version string if available, otherwise undefined.

**Throws:**
- If an error occurs while connecting to the Kiota service or retrieving the version.

### `getManifestDetails`

Retrieves the manifest details for a given API.

**Parameters:**

- `options`: *ManifestOptions* - The options for retrieving the manifest details.
- `options.manifestPath`: *string* - The path to the manifest file.

- `options.clearCache` (optional): *boolean* - Whether to clear the cache before retrieving the manifest details.
- `options.apiIdentifier` (optional): *string* - The identifier of the API.

**Returns:** A promise that resolves to the manifest details or undefined if not found.

**Throws:**
- Throws an error if the request fails.

### `getPluginManifest`

Shows the Kiota result based on the provided options.

**Parameters:**

- `options`: *KiotaResultOptions* - The options to configure the Kiota result.
- `options.descriptionPath`: *string* - The path to the manifest file.

**Returns:** A promise that resolves to the result or undefined if an error occurs.

**Throws:**
- Throws an error if the result is an instance of Error.

### `getLanguageInformationInternal`

Retrieves language information by connecting to Kiota.

This function establishes a connection to Kiota and sends a request to retrieve
language information. If the request is successful, it returns the language information.
If an error occurs during the request, the error is thrown.

**Returns:** A promise that resolves to the language information or undefined if an error occurs.

**Throws:**
- Throws an error if the request fails.

### `getLanguageInformationForDescription`

Retrieves language information based on the provided description URL.

**Parameters:**

- `config`: *LanguageInformationConfiguration* - The configuration object containing the description URL and cache clearing option.
- `config.descriptionUrl`: *string* - The URL of the description to retrieve language information for.
- `config.clearCache`: *boolean* - A flag indicating whether to clear the cache before retrieving the information.

**Returns:** A promise that resolves to the language information or undefined if an error occurs.

**Throws:**
- Throws an error if the request fails.

### `migrateFromLockFile`

Migrates data from a lock file located in the specified directory.

This function connects to the Kiota service and sends a request to migrate data from the lock file.
If the migration is successful, it returns an array of `KiotaLogEntry` objects.
If an error occurs during the migration, the error is thrown.

**Parameters:**

- `lockFileDirectory`: *string* - The directory where the lock file is located.

**Returns:** A promise that resolves to an array of `KiotaLogEntry` objects if the migration is successful, or `undefined` if no data is migrated.

**Throws:**
- If an error occurs during the migration process.

### `removePlugin`

Removes a plugin from the Kiota environment.

**Parameters:**

- `config`: *RemovePluginConfiguration* - The configuration for removing the plugin.
- `config.pluginName`: *string* - The name of the plugin to remove.
- `config.cleanOutput`: *boolean* - Whether to clean the output directory after removal.
- `config.workingDirectory`: *string* - The working directory where the operation should be performed.

**Returns:** A promise that resolves to a KiotaResult if the operation is successful, or undefined if no result is returned.

**Throws:**
- Throws an error if the operation fails.

### `removeClient`

Removes a client using the provided configuration.

**Parameters:**

- `config`: *RemoveClientConfiguration* - The configuration for removing the client.
- `config.clientName`: *string* - The name of the client to be removed.
- `config.cleanOutput`: *boolean* - A flag indicating whether to clean the output.
- `config.workingDirectory`: *string* - The working directory for the operation.

**Returns:** A promise that resolves to a KiotaResult if the client was removed successfully, or undefined if no result is returned.

**Throws:**
- Throws an error if the result is an instance of Error.

### `searchDescription`

Searches for a description based on the provided search term and cache settings.

**Parameters:**

- `config`: *SearchConfiguration* - The search configuration object.
- `config.searchTerm`: *string* - The term to search for.
- `config.clearCache`: *boolean* - Whether to clear the cache before searching.

**Returns:** A promise that resolves to a record of search results or undefined if no results are found.

**Throws:**
- Throws an error if the search operation fails.

### `updateClients`

Updates the clients by connecting to Kiota and sending a request to update.

**Parameters:**

- `config`: *UpdateClientsConfiguration* - The configuration object containing the following properties:
- `config.cleanOutput`: *boolean* - Whether to clean the output directory before updating.
- `config.clearCache`: *boolean* - Whether to clear the cache before updating.
- `config.workspacePath`: *string* - The path to the workspace where the clients are located.

**Returns:** A promise that resolves to an array of Kiota log entries if the update is successful, or undefined if there is an error.

**Throws:**
- Throws an error if the result of the update is an instance of Error.

