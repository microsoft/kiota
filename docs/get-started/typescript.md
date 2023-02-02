---
parent: Get started
---

# Build SDKs for TypeScript

## Required tools

- [NodeJS 18](https://nodejs.org/en/)
- [TypeScript](https://www.typescriptlang.org/)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to install dependencies of the [abstractions](https://www.npmjs.com/package/@microsoft/kiota-abstractions), [authentication](https://www.npmjs.com/package/@microsoft/kiota-authentication-azure), [http fetch](https://www.npmjs.com/package/@microsoft/kiota-http-fetchlibrary), [serialization form](https://www.npmjs.com/package/@microsoft/kiota-serialization-form), [serialization json](https://www.npmjs.com/package/@microsoft/kiota-serialization-json) and [serialization text](https://www.npmjs.com/package/@microsoft/kiota-serialization-text) packages from the npm registry.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following commands in the directory where you want to create a new project.

```bash
npm init
npm install -D typescript ts-node
npx tsc --init
```

## Adding dependencies

The following packages can be added to the project.

```bash
npm install @microsoft/kiota-abstractions
npm install @microsoft/kiota-authentication-azure
npm install @microsoft/kiota-http-fetchlibrary
npm install @microsoft/kiota-serialization-form
npm install @microsoft/kiota-serialization-json
npm install @microsoft/kiota-serialization-text
npm install @azure/identity node-fetch
```

Only the first package, `kiota-abstractions`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```bash
kiota generate -l typescript -d getme.yml -c GetUserApiClient -o ./client
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Create a file in the root of the project named **index.ts** and add the following code. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```typescript
import { DeviceCodeCredential } from '@azure/identity';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { GetUserApiClient } from './client/getUserApiClient';

const clientId = 'YOUR_CLIENT_ID';

// The auth provider will only authorize requests to
// the allowed hosts, in this case Microsoft Graph
const allowedHosts = new Set<string>([ 'graph.microsoft.com' ]);
const graphScopes = [ 'User.Read' ];

const credential = new DeviceCodeCredential({
  clientId: clientId,
  userPromptCallback: (deviceCodeInfo) => {
    console.log(deviceCodeInfo.message);
  }
});

const authProvider =
  new AzureIdentityAuthenticationProvider(credential, graphScopes, undefined, allowedHosts);
const adapter = new FetchRequestAdapter(authProvider);

const client = new GetUserApiClient(adapter);

async function GetUser(): Promise<void> {
  try {
    const me = await client.me.get();
    console.log(`Hello ${me?.displayName}, your ID is ${me?.id}`);
  } catch (err) {
    console.log(err);
  }
}

GetUser();
```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API relies on an API key for authentication, you can use the **ApiKeyAuthenticationProvider** instead.
> - If the target API requires an `Authorization: Bearer <token>` header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```bash
npx ts-node index.ts
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/typescript) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
