namespace Kiota.Builder.Tests.OpenApiSampleFiles;


public static class PetsUnion
{
    /**
    * An OpenAPI 3.0.1 sample document with a union of objects, comprising a union of Cats and Dogs.
    */
    public static readonly string OpenApiYaml = @"
openapi: 3.0.0
info:
  title: Pet API
  version: 1.0.0
paths:
  /pets:
    patch:
      summary: Update a pet
      requestBody:
        required: true
        content:
          application/json:
            schema:
              oneOf:
                - $ref: '#/components/schemas/Cat'
                - $ref: '#/components/schemas/Dog'
              discriminator:
                propertyName: pet_type
      responses:
        '200':
          description: Updated
components:
  schemas:
    Pet:
      type: object
      required:
        - pet_type
      properties:
        pet_type:
          type: string
      discriminator:
        propertyName: pet_type
    Dog:     
      allOf: 
        - $ref: '#/components/schemas/Pet'
        - type: object
          properties:
            bark:
              type: boolean
            breed:
              type: string
              enum: [Dingo, Husky, Retriever, Shepherd]
      required:
        - pet_type
        - bark
        - breed
    Cat:     
      allOf: 
        - $ref: '#/components/schemas/Pet'
        - type: object
          properties:
            hunts:
              type: boolean
            age:
              type: integer
      required:
        - pet_type
        - hunts
        - age";
}
