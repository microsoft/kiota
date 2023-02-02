---
parent: Get started
---

# Build SDKs for Java

## Required tools

- [JDK 16](https://adoptopenjdk.net/)
- [Gradle 7.4](https://gradle.org/install/)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota-java), [authentication](https://github.com/microsoft/kiota-java), [http](https://github.com/microsoft/kiota-java), [serialization Form](https://github.com/microsoft/kiota-java), [serialization JSON](https://github.com/microsoft/kiota-java), and [serialization Text](https://github.com/microsoft/kiota-java) packages.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Use Gradle to initialize a Java application project.

```bash
gradle init --dsl groovy --test-framework junit --type java-application --project-name getuserclient --package getuserclient
```

## Adding dependencies

### Getting access to the packages

### Add Kiota and dependencies

Edit **./app/build.gradle** to add the following dependencies.

> **Note:** Find current version numbers for Kiota packages at [Nexus Repository Manager](https://oss.sonatype.org/).

```groovy
implementation 'com.microsoft.kiota:microsoft-kiota-abstractions:0.2.0'
implementation 'com.microsoft.kiota:microsoft-kiota-authentication-azure:0.2.0'
implementation 'com.microsoft.kiota:microsoft-kiota-http-okHttp:0.2.0'
implementation 'com.microsoft.kiota:microsoft-kiota-serialization-json:0.2.0'
implementation 'com.microsoft.kiota:microsoft-kiota-serialization-text:0.2.0'
implementation 'com.microsoft.kiota:microsoft-kiota-serialization-form:0.2.0'
implementation 'com.azure:azure-identity:1.7.3'
```

Only the first package, `kiota-abstractions`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```bash
kiota generate -l java -d getme.yml -c GetUserApiClient -n getuserclient.apiclient -o ./app/src/main/java/getuserclient/apiclient
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
> - If the target API relies on an API key for authentication, you can use the **ApiKeyAuthenticationProvider** instead.
> - If the target API requires an `Authorization bearer <token>` header but doesn't rely on the Microsoft identity platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```bash
./gradlew --console plain run
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/java) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
