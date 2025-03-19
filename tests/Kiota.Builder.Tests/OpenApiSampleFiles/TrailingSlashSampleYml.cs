namespace Kiota.Builder.Tests.OpenApiSampleFiles;

public static class TrailingSlashSampleYml
{
    /**
    * An OpenAPI 3.0.0 sample document with trailing slashes on some paths.
    */
    public static readonly string OpenApiYaml = @"
openapi: 3.0.0
info:
  title: Sample API
  description: A sample API that uses trailing slashes.
  version: 1.0.0
servers:
  - url: https://api.example.com/v1
paths:
  /foo:
    get:
      summary: Get foo
      description: Returns foo.
      responses:
        '200':
          description: foo
          content:
            text/plain:
              schema:
                type: string
  /foo/:
    get:
      summary: Get foo slash
      description: Returns foo slash.
      responses:
        '200':
          description: foo slash
          content:
            text/plain:
              schema:
                type: string
  /message/{id}:
    get:
      summary: Get a Message
      description: Returns a single Message object.
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: A Message object
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Message'
  /message/{id}/:
    get:
      summary: Get replies to a Message
      description: Returns a list of Message object replies for a Message.
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: A list of Message objects
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Message'
  /bucket/{name}/:
    get:
      summary: List items in a bucket
      description: Returns a list of BucketFiles in a bucket.
      parameters:
        - name: name
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: A list of BucketFile objects
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/BucketFile'
  /bucket/{name}/{id}:
    get:
      summary: Get a bucket item
      description: Returns a single BucketFile object.
      parameters:
        - name: name
          in: path
          required: true
          schema:
            type: string
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: A BucketFile object
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/BucketFile'
components:
  schemas:
    Message:
      type: object
      properties:
        Guid:
          type: string
      required:
        - Guid
    BucketFile:
      type: object
      properties:
        Guid:
          type: string
      required:
        - Guid";
}
