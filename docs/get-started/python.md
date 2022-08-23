---
parent: Get started
---

# Build SDKs for Python

## Required tools

- [Python 3.10.4](https://www.python.org/)
- [pip 20.0.2](https://pip.pypa.io/en/stable/)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota-abstractions-python), [authentication](https://github.com/microsoft/kiota-authentication-azure-python), [http](https://github.com/microsoft/kiota-http-python), [serialization JSON](https://github.com/microsoft/kiota-serialization-json-python), and [serialization Text](https://github.com/microsoft/kiota-serialization-text-python) packages.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following commands in the directory where you want to create a new project.

```bash
py someCommand
```

## Adding dependencies

Create a file named **getuser.py** and add the following code.

```bash
py get github.com/microsoft/microsoft-kiota-abstractions-python
py get github.com/microsoft/microsoft-kiota-http-python
py get github.com/microsoft/microsoft-kiota-serialization-json-python
py get github.com/microsoft/microsoft-kiota-serialization-text-python
py get github.com/microsoft/kiota-authentication-azure-python
py get github.com/Azure/azure-sdk-for-python/sdk/azidentity
```

Only the first package, `github.com/microsoft/microsoft-kiota-abstractions-python`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota -l py -d ../getme.yml -c GraphApiClient -n getuser/client -o ./client
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Create a file in the root of the project named **getuser.py** and add the following code. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```python
code goes here
```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API requires an `Authorization: Bearer <token>` header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```shell
py run .
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/python) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
