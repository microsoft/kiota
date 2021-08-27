# Kiota SDK Experience

## API Style

Basic read and write syntax for a resource.

```csharp
var authProvider = ; /** An authentication provider from the supported language table https://github.com/microsoft/kiota#supported-languages, or your own implementation **/
var coreService = new HttpCore(authProvider);
var client = new ApiClient(coreService);
var user = await client.Users["bob@contoso.com"].GetAsync();

var newUser = new User {
    FirstName = "Bill",
    LastName = "Brown"
}

await client.Users.PostAsync(newUser)

```

Resources are accessed via relation properties starting from the client object.  Collections of resources can be accessed by an indexer and a parameter. Once the desired resource has been referenced, the supported HTTP methods are exposed by corresponding methors.  Deeply nested resource hierarchy can be accessed by continuing to traverse relationships.

```csharp
var authProvider = ; /** An authentication provider from the supported language table https://github.com/microsoft/kiota#supported-languages, or your own implementation **/
var coreService = new HttpCore(authProvider);
var client = new ApiClient(coreService);
var message = await client.Users["bob@contoso.com"]
                          .MailFolders["Inbox"]
                          .Messages[23242]
                          .GetAsync();

```

The client object is a [request builder](requestbuilders) object, and forms the root of a hierarchy of request builder objects that can access any number of APIs that are merged into a common URI space.

Requests can be further refined by providing query parameters. Each HTTP operation method that supports query parameters accepts a lambda that can configure an object with the desired query parameters.

```csharp
var authProvider = ; /** An authentication provider from the supported language table https://github.com/microsoft/kiota#supported-languages, or your own implementation **/
var coreService = new HttpCore(authProvider);
var client = new ApiClient(coreService);
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {  q.StartDateTime = DateTime.Now;
                                            q.EndDateTime = DateTime.Now.AddDays(7);
                                        });

```

Using a configured query parameter object prevents tight coupling on the order of query parameters and make optional parameters easy to implement across languages.