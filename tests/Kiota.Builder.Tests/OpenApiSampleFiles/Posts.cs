namespace Kiota.Builder.Tests.OpenApiSampleFiles;


public static class Posts
{
    /**
    * An OpenAPI 3.0.1 sample document with a union of objects, comprising a union of Cats and Dogs.
    */
    public static readonly string OpenApiYaml = @"openapi: '3.0.2'
info:
  title: JSONPlaceholder
  version: '1.0'
servers:
  - url: https://jsonplaceholder.typicode.com/

components:
  schemas:
    post:
      type: object
      properties:
        userId:
          type: integer
        id:
          type: integer
        title:
          type: string
        body:
          type: string
  parameters:
    post-id:
      name: post-id
      in: path
      description: 'key: id of post'
      required: true
      style: simple
      schema:
        type: integer

paths:
  /posts:
    get:
      description: Get posts
      operationId: list-posts
      parameters:
      - name: userId
        in: query
        description: Filter results by user ID
        required: false
        style: form
        schema:
          type: integer
          maxItems: 1
      - name: title
        in: query
        description: Filter results by title
        required: false
        style: form
        schema:
          type: string
          maxItems: 1
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/post'
    post:
      description: 'Create post'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/post'
      responses:
        '201':
          description: Created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/post'
  /posts/{post-id}:
    get:
      description: 'Get post by ID'
      parameters:
      - $ref: '#/components/parameters/post-id'
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/post'
    patch:
      description: 'Update post'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/post'
      parameters:
      - $ref: '#/components/parameters/post-id'
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/post'
    delete:
      description: 'Delete post'
      parameters:
      - $ref: '#/components/parameters/post-id'
      responses:
        '200':
          description: OK
";
}
