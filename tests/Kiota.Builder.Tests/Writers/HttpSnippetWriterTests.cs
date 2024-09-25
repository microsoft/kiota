using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers;
public sealed class HttpSnippetWriterTests : IDisposable
{
    private readonly StringWriter writer;
    private readonly HttpClient _httpClient = new();

    public HttpSnippetWriterTests()
    {
        writer = new StringWriter();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        writer?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WritesHttpSnippetAsync()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, postsOpenAPIDescription);
        var mockLogger = new Mock<ILogger<HttpSnippetGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath"
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);
        var httpSnippetWriter = new HttpSnippetWriter(writer);
        httpSnippetWriter.Write(urlTreeNode);

        var result = writer.ToString();

        Assert.Contains("GET {{url}}/posts/{{post-id}} HTTP/1.1", result);
        Assert.Contains("PATCH {{url}}/posts/{{post-id}} HTTP/1.1", result);
        Assert.Contains("Content-Type: application/json", result);
        Assert.Contains("\"userId\": \"userId\"", result);
        Assert.Contains("\"id\": \"id\",", result);
        Assert.Contains("\"title\": \"title\",", result);
        Assert.Contains("\"title\": \"title\",", result);
        Assert.Contains("\"body\": \"body\"", result);
        Assert.Contains("}", result);
    }

    private readonly string postsOpenAPIDescription = @"openapi: '3.0.2'
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
          description: OK"; 
}
