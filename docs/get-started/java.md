---
parent: Get started
---

# Build SDKs for Java

## Required tools

- [JDK 16](https://adoptopenjdk.net/)
- [Gradle 7](https://gradle.org/install/)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota/tree/main/abstractions/java), [authentication](https://github.com/microsoft/kiota/tree/main/authentication/java/azure), [http](https://github.com/microsoft/kiota/tree/main/http/java/okhttp), and [serialization](https://github.com/microsoft/kiota/tree/main/serialization/java/json) packages from the GitHub feed.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Use Gradle to initialize a Java application project.

```bash
gradle init --dsl groovy --test-framework junit --type java-application --project-name getuserclient --package getuserclient
```

## Adding dependencies

### Getting access to the packages

> **Note:** This is a temporary requirement while Kiota is in preview.

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: `package:read`, `repo`).
1. Copy the token, you will need it later.
1. Enable SSO for the Microsoft org on the token if you are a Microsoft employee.

1. Create a new file **./gradle.properties**. Add the following contents.

    ```groovy
    gpr.user = YOUR_GITHUB_USERNAME
    gpr.key = YOUR_PERSONAL_ACCESS_TOKEN
    ```

    > **Note:** Add **gradle.properties** to your **.gitignore** to avoid committing your PAT!

1. Open **./app/build.gradle** and add the following inside the `repositories` value.

    ```groovy
    maven {
        url = uri("https://maven.pkg.github.com/microsoft/kiota")
        credentials {
            username = project.findProperty("gpr.user") ?: System.getenv("USERNAME")
            password = project.findProperty("gpr.key") ?: System.getenv("TOKEN")
        }
    }
    ```

### Add Kiota and dependencies

Edit **./app/build.gradle** to add the following dependencies.

> **Note:** Find current version numbers for Kiota packages at [GitHub](https://github.com/orgs/microsoft/packages?repo_name=kiota&ecosystem=maven).

```groovy
implementation 'com.microsoft.kiota:kiota-abstractions:1.0.27'
implementation 'com.microsoft.kiota.serialization:kiota-serialization-json:1.0.7'
implementation 'com.microsoft.kiota.authentication:kiota-authentication-azure:1.0.6'
implementation 'com.microsoft.kiota.http:kiota-http-okhttplibrary:1.0.15'
implementation 'com.azure:azure-identity:1.4.4'
```

Only the first package, `kiota-abstractions`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota -l java -d getme.yml -c GetUserApiClient -n getuserclient.apiclient -o ./app/src/main/java/getuserclient/apiclient
```

## Registering an application in Azure AD

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

The final step is to update the **./app/src/main/java/getuserclient/App.java** file that was generated as part of the console application to include the code below. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```java
package getuserclient;

import com.azure.identity.DeviceCodeCredential;
import com.azure.identity.DeviceCodeCredentialBuilder;
import com.microsoft.kiota.authentication.AzureIdentityAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;

import getuserclient.apiclient.GetUserApiClient;

public class App {

    public static void main(String[] args) {
        final String clientId = "YOUR_CLIENT_ID";

        // The auth provider will only authorize requests to
        // the allowed hosts, in this case Microsoft Graph
        final String[] allowedHosts = new String[] { "graph.microsoft.com" };
        final String[] graphScopes = new String[] { "User.Read" };

        final DeviceCodeCredential credential = new DeviceCodeCredentialBuilder()
            .clientId(clientId)
            .challengeConsumer(challenge -> System.out.println(challenge.getMessage()))
            .build();


        final AzureIdentityAuthenticationProvider authProvider =
            new AzureIdentityAuthenticationProvider(credential, allowedHosts, graphScopes);
        final OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(authProvider);

        final GetUserApiClient client = new GetUserApiClient(adapter);

        client.me()
            .get()
            .thenAccept(me -> {
                System.out.printf("Hello %s, your ID is %s%n",
                    me.getDisplayName(), me.getId());
            })
            .exceptionally(err -> {
                System.out.printf("Error: %s%n", err.getMessage());
                return null;
            })
            .join();
    }
}
```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API requires an `Authorization bearer <token>` header but doesn't rely on the Microsoft identity platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```shell
./gradlew --console plain run
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/java) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
