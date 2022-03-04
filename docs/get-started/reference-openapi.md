---
title: Sample OpenAPI
parent: Get started
---

# Sample OpenAPI description

The following is a minimal OpenAPI description that describes how to call the `/me` endpoint on Microsoft Graph. This description is used in the following guides.

- [Build SDKs for .NET](dotnet.md)
- [Build SDKs for Go](go.md)
- [Build SDKs for Java](java.md)
- [Build SDKs for TypeScript](typescript.md)

```yaml
openapi: 3.0.3
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/microsoft.graph.user"
components:
  schemas:
    microsoft.graph.user:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string
```
