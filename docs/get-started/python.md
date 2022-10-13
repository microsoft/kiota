---
parent: Get started
---

# Build SDKs for Python

## Required tools

- [Python 3.6+](https://www.python.org/)
- [pip 20.0+](https://pip.pypa.io/en/stable/)
- [Asyncio/any other supported async envronment e.g AnyIO, Trio.](https://docs.python.org/3/library/asyncio.html)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota-abstractions-python), [authentication](https://github.com/microsoft/kiota-authentication-azure-python), [http](https://github.com/microsoft/kiota-http-python), [serialization JSON](https://github.com/microsoft/kiota-serialization-json-python), and [serialization Text](https://github.com/microsoft/kiota-serialization-text-python) packages.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Create a directory that will contain the new project.

## Adding dependencies
In your project directory, run the following commands on the terminal to install required dependencies
using `pip`:

```bash
pip install microsoft-kiota-abstractions
pip install microsoft-kiota-authentication-azure
pip install azure-identity
pip install microsoft-kiota-serialization-json
pip install microsoft-kiota-serialization-text
pip install microsoft-kiota-http
```

> **Note:** It is recommended to use a package manager/virtual environment to avoid installing packages
system wide. Read more [here](https://packaging.python.org/en/latest/).

Only the first package, `microsoft-kiota-abstractions`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota generate -l python -d ../getme.yml -c GetUserApiClient -n getuser/client -o ./client
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Create a file in the root of the project named **get_user.py** and add the following code. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```py
import asyncio
from azure.identity.aio import DefaultAzureCredential

from kiota_authentication_azure.azure_identity_authentication_provider import AzureIdentityAuthenticationProvider
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from kiota_serialization_json.json_parse_node_factory import JsonParseNodeFactory
from kiota_serialization_json.json_serialization_writer_factory import JsonSerializationWriterFactory

from client.get_user_api_client import GetUserApiClient

# You may need this if your're using AsyncIO on windows
# See: https://stackoverflow.com/questions/63860576/asyncio-event-loop-is-closed-when-using-asyncio-run
asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

credential = DefaultAzureCredential()
# The auth provider will only authorize requests to
# the allowed hosts, in this case Microsoft Graph

allowed_hosts = ['graph.microsoft.com']
graph_scopes = ['https://graph.microsoft.com/.default']
auth_provider = AzureIdentityAuthenticationProvider(credential, None, graph_scopes, allowed_hosts)


request_adapter = HttpxRequestAdapter(auth_provider)
client = GetUserApiClient(request_adapter)

me = asyncio.run(client.me().get())
print(f"Hello {me.displayName}, your ID is {me.id}")
```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API requires an `Authorization: Bearer <token>` header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```shell
python get_user.py
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/python) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
