namespace Kiota.Builder.Tests.OpenApiSampleFiles;

public static class CodeIntersectionTypeSampleYml
{
    public static readonly string OpenApiYaml = @"
openapi: 3.0.3
info:
  title: FooBar API
  description: A sample API that returns an object FooBar which is an intersection of Foo and Bar.
  version: 1.0.0
servers:
  - url: https://api.example.com/v1
paths:
  /foobar:
    get:
      summary: Get a FooBar object
      description: Returns an object that is an intersection of Foo and Bar.
      responses:
        '200':
          description: A FooBar object
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/FooBar'
components:
  schemas:
    Foo:
      type: object
      properties:
        foo:
          type: string
      required:
        - foo
    Bar:
      type: object
      properties:
        bar:
          type: string
      required:
        - bar
    FooBar:
      anyOf:
        - $ref: '#/components/schemas/Foo'
        - $ref: '#/components/schemas/Bar'";
}
