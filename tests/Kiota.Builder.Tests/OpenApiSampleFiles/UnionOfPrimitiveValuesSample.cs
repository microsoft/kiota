namespace Kiota.Builder.Tests.OpenApiSampleFiles;


public static class UnionOfPrimitiveValuesSample
{
    /**
    * An OpenAPI 3.0.1 sample document with a union of primitive values, comprising a union of stings and numbers.
    */
    public static readonly string Yaml = @"
openapi: 3.0.1
info:
  title: Example of UnionTypes
  version: 1.0.0
paths:
  /primitives:
    get:
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/primitives'
components:
  schemas:
    primitives:
      oneOf:
        - type: string
        - type: number";

}
