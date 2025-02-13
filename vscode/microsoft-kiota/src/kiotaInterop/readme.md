# Kiota Interop Library

This library provides various functions to interact with Kiota, a client generator for HTTP REST APIs described by OpenAPI.

## Installation
Provide instructions on how to install the project.

```bash
npm install kiota-Interop
```

## Usage

### [`generateClient`](./lib/generateClient.ts )

Generates a client based on the provided client generation options.

### [`generatePlugin`](./lib/generatePlugin.ts )

The function connects to Kiota and sends a request to generate a plugin using the provided options.
It handles the response and checks for success, returning the result or throwing an error if one occurs.

### [`getKiotaVersion`](./lib/getKiotaVersion.ts )

Retrieves the version of Kiota by connecting to the Kiota service.


### [`getManifestDetails`](./lib/getManifestDetails.ts )

Retrieves the manifest details for a given API.

### [`getLanguageInformationInternal`](./lib/languageInformation.ts )

Retrieves language information by connecting to Kiota

### [`getLanguageInformationForDescription`](./lib/languageInformation.ts )

Retrieves language information based on the provided description URL.

### [`migrateFromLockFile`](./lib/migrateFromLockFile.ts )

Migrates data from a lock file located in the specified directory.

### [`removePlugin`](./lib/removeItem.ts )

Removes a plugin from the Kiota environment.

### [`removeClient`](./lib/removeItem.ts )
Removes a client using the provided configuration.

### [`searchDescription`](./lib/searchDescription.ts )
Searches for a description based on the provided search term and cache settings.

### [`showKiotaResult`](/kiotaInterop./lib/showKiotaResult.ts )
Shows the Kiota result based on the provided options.

### [`updateClients`](./lib/updateClients.ts )
Shows the Kiota result based on the provided options.

