namespace Kiota.Builder.Tests.OpenApiSampleFiles;

public static class UnionOfPrimitiveAndObjects
{
    /**
    * An OpenAPI 3.0.1 sample document with union between objects and primitive types
    */
    public static readonly string openApiSpec = @"
openapi: 3.0.3
info:
  title: Pet API
  description: An API to return pet information.
  version: 1.0.0
servers:
  - url: http://localhost:8080
    description: Local server

paths:
  /pet:
    get:
      summary: Get pet information
      operationId: getPet
      responses:
        '200':
          description: Successful response
          content:
            application/json:
              schema:
                type: object
                properties:
                  request_id:
                    type: string
                    example: ""123e4567-e89b-12d3-a456-426614174000""
                  data:
                    oneOf:
                      - $ref: '#/components/schemas/Cat'
                      - $ref: '#/components/schemas/Dog'
                      - type: string
                        description: Error message
                        example: ""An error occurred while processing the request.""
                      - type: integer
                        description: Error code
                        example: 409
        '400':
          description: Bad Request
        '500':
          description: Internal Server Error

components:
  schemas:
    Pet:
      type: object
      required:
        - name
        - age
      properties:
        name:
          type: string
          example: ""Fluffy""
        age:
          type: integer
          example: 4

    Cat:
      allOf:
        - $ref: '#/components/schemas/Pet'
        - type: object
          properties:
            favoriteToy:
              type: string
              example: ""Mouse""

    Dog:
      allOf:
        - $ref: '#/components/schemas/Pet'
        - type: object
          properties:
            breed:
              type: string
              example: ""Labrador""
";

}
