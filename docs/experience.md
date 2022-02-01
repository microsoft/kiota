---
parent: Welcome to Kiota
nav_order: 1
---

# Kiota SDK Experience

## Create a Resource

Basic read and write syntax for a resource.

```csharp
// An authentication provider from the supported language table
// https://github.com/microsoft/kiota#supported-languages, or your own implementation
var authProvider = ;
var requestAdapter = new HttpClientRequestAdapter(authProvider);
var client = new ApiClient(requestAdapter);
var user = await client.Users["bob@contoso.com"].GetAsync();

var newUser = new User
{
    FirstName = "Bill",
    LastName = "Brown"
};

await client.Users.PostAsync(newUser);
```

## Access Related Resources

Resources are accessed via relation properties starting from the client object.  Collections of resources can be accessed by an indexer and a parameter. Once the desired resource has been referenced, the supported HTTP methods are exposed by corresponding methors.  Deeply nested resource hierarchy can be accessed by continuing to traverse relationships.

```csharp
// An authentication provider from the supported language table
// https://github.com/microsoft/kiota#supported-languages, or your own implementation
var authProvider = ;
var requestAdapter = new HttpClientRequestAdapter(authProvider);
var client = new ApiClient(requestAdapter);
var message = await client.Users["bob@contoso.com"]
                          .MailFolders["Inbox"]
                          .Messages[23242]
                          .GetAsync();
```

The client object is a [request builder](extending/requestbuilders.md) object, and forms the root of a hierarchy of request builder objects that can access any number of APIs that are merged into a common URI space.

Requests can be further refined by providing query parameters. Each HTTP operation method that supports query parameters accepts a lambda that can configure an object with the desired query parameters.

```csharp
// An authentication provider from the supported language table
// https://github.com/microsoft/kiota#supported-languages, or your own implementation
var authProvider = ;
var requestAdapter = new HttpClientRequestAdapter(authProvider);
var client = new ApiClient(requestAdapter);
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {  q.StartDateTime = DateTime.Now;
                                            q.EndDateTime = DateTime.Now.AddDays(7);
                                        });
```

Using a configured query parameter object prevents tight coupling on the order of query parameters and make optional parameters easy to implement across languages.

## Response with no schema

```yaml
openapi: 3.0.3
info:
  title: The simplest thing that works
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /speakers:
    get: 
      responses:
        200:
          description: Ok
```

If the OpenAPI description does not describe the response payload, then it should be assumed to be of content type `application/octet-stream`.

SDK implementations should return the response payload in the language-native way of providing an untyped set of bytes. This could be a byte-array or some kind of stream response.  

```csharp

   Stream speaker = await apiClient.Speakers.GetAsync();

```

> **Warning:** Support for stream responses identified by application/octet-stream are not supported yet

## Response with simple schema

```yaml
openapi: 3.0.3
info:
  title: Response with simple schema
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /speakers/{speakerId}:
    get: 
      parameters:
        - name: speakerId
          in: path
          required: true
          schema:
            type: string
      responses:
        200:
          description: Ok
          content:
            application/json:
              schema:
                type: object
                properties: 
                  displayName: 
                    type: string
```

```csharp

   Speaker speaker = await apiClient.Speakers["23"].GetAsync();
   string displayName = speaker.DisplayName;

```

## Response with primitive payload

Responses with a content type of `text/plain` should be serialized into a primitive data type.  Unless a Schema object indicates a more precise data type, the payload should be serialized to a string.

```yaml
openapi: 3.0.3
info:
  title: Response with primitive payload
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /speakers/count:
    get: 
      responses:
        200:
          description: Ok
          content:
            text/plain:
              schema:
                type: number
```

```csharp

   int speakerCount = await apiClient.Speakers.Count;

```

> **Warning:** Support for `text/plain` responses are not supported yet

## Filtered collection

```yaml
openapi: 3.0.3
info:
  title: Collection filtered by query parameter
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /speakers:
    get: 
      parameters:
        - name: location
          in: query
          required: false
          schema:
            type: string
      responses:
        200:
          description: Ok
          content:
            application/json:
              schema:
                type: array
                item:
                  $ref: "#/components/schemas/speaker"
components:
  schemas:
    speaker:
      type: object
      properties: 
        displayName: 
          type: string
        location:
          type: string
```

```csharp

   IEnumerable<Speaker> speakers = await apiClient.Speakers.GetAsync(new { Location="Montreal" });

```

## Hetreogenous collection

```yaml
## Filtered collection

```yaml
openapi: 3.0.3
info:
  title: Hetreogenous collection
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /sessions:
    get: 
      parameters:
        - name: location
          in: query
          required: false
          schema:
            type: string
      responses:
        200:
          description: Ok
          content:
            application/json:
              schema:
                type: array
                item:
                  $ref: "#/components/schemas/session"
components:
  schemas:
    entity:
      type: object
      properties:
        id: 
          type: string
    session:
      allof:
        - $ref: "#/components/schemas/entity"
      type: object
      properties: 
        '@OData.Type': 
          type: string
          enum:
           - session
        displayName: 
          type: string
        location:
          type: string
    workshop:
      allof:
        - $ref: "#/components/schemas/session"
      type: object
      properties: 
        '@OData.Type': 
          type: string
          enum:
           - workshop
        requiredEquipment: 
          type: string
    presentation:
      allof:
        - $ref: "#/components/schemas/session"
      type: object
      properties: 
        '@OData.Type': 
          type: string
          enum:
           - presentation
        recorded: 
          type: boolean

```

```csharp
   IEnunmerable<Session> sessions = await apiClient.Sessions.GetAsync(); 
   List<Presentation> presentations = sessions.Where(s => s.GetType() == typeOf(Presentation)).ToList();
```

## Explicit Error Response

```yaml
openapi: 3.0.3
info:
  title: The simplest thing that works
  version: 1.0.0
servers:
  - url: https://example.org/
paths:
  /speakers:
    get: 
      responses:
        "2XX":
          description: Success
        "4XX":
          $ref: "#/components/responses/errorResponse"
        "5XX":
          $ref: "#/components/responses/errorResponse"
components:
  responses: 
    errorResponse:
      description: error
      content:
        application/json:
          schema:
            type: object
            properties:
              code: 
                type: string
              message:
                type: string
```

```csharp
  try {
   var speakersStream = await apiClient.Speakers.GetAsync();
  } 
  catch ( ServerException exception ) {
    Console.WriteLine(exception.Error.Message)
  } 
```

> **Warning:** Support for ServerException is not supported yet
