---
parent: Get started
---

# Build SDKs for PHP

## Required tools

- [PHP ^7.4 or ^8.0](https://www.php.net/downloads)

## Target project requirements

Before you can run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstractions](https://github.com/microsoft/kiota-abstractions-php), [authentication](https://github.com/microsoft/kiota-authentication-phpleague-php), [http](https://github.com/microsoft/kiota-http-guzzle-php), [serialization JSON](https://github.com/microsoft/kiota-serialization-json-php), and [serialization Text](https://github.com/microsoft/kiota-serialization-text-php) packages.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following commands in the directory where you want to create a new project.

```bash
composer init
```

## Adding dependencies

```bash
composer require microsoft/kiota-abstractions
composer require microsoft/kiota-http-guzzle
composer require microsoft/kiota-authentication-phpleague
composer require microsoft/kiota-serialization-json
composer require microsoft/kiota-serialization-text
```

Add the following to your `composer.json` to set your namespaces correctly:
```shell
"autoload": {
    "psr-4": {
        "GetUser\\Client\\": "client/"
    }
}
```

Only the first package, `microsoft/kiota-abstractions`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota -l PHP -d ../getme.yml -c GraphApiClient -n GetUser\Client -o ./client
```

To ensure the newly generated classes can be imported, update the autoload paths using:

```shell
composer dumpautoload
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Create a file in the root of the project named **GetUser.php** and add the following code. Replace the `$tenantId`, `$clientId`, `$clientSecret`
with your credentials from the previous step. See [Get access on behalf of a user](https://docs.microsoft.com/en-us/graph/auth-v2-user?context=graph%2Fapi%2F1.0&view=graph-rest-1.0) for details on
how to get the `$authorizationCode` and set the `$redirectUri`.

```php
<?php

use GetUser\Client\GraphApiClient;
use Microsoft\Kiota\Abstractions\ApiException;
use Microsoft\Kiota\Authentication\Oauth\AuthorizationCodeContext;
use Microsoft\Kiota\Authentication\PhpLeagueAuthenticationProvider;
use Microsoft\Kiota\Http\GuzzleRequestAdapter;

require __DIR__.'/vendor/autoload.php';

try {
    $tenantId = 'tenantId';
    $clientId = 'clientId';
    $clientSecret = 'secret';
    $authorizationCode = 'authCode';
    $redirectUri = 'uri';

    $scopes = ['User.Read'];

    $tokenRequestContext = new AuthorizationCodeContext(
        $tenantId,
        $clientId,
        $clientSecret,
        $authorizationCode,
        $redirectUri
    );

    $authProvider = new PhpLeagueAuthenticationProvider($tokenRequestContext, $scopes);
    $requestAdapter = new GuzzleRequestAdapter($authProvider);
    $client = new GraphApiClient($requestAdapter);

    $me = $client->me()->get()->wait();
    echo "Hello {$me->getDisplayName()}, your ID is {$me->getId()}";

} catch (ApiException $ex) {
    echo $ex->getMessage();
}

```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API requires an `Authorization: Bearer <token>` header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```shell
php GetUser.php
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/php) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
