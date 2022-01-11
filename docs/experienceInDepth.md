---
parent: Welcome to Kiota
nav_order: 1
---

# Kiota SDK Experience In Depth

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
  title: Collection filtered by query parameter
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
   Presentation presentation = await apiClient.Sessions["idOfPresentation"].GetAsync() as Presentation; 
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
