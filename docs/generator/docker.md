---
parent: Generators
---

# Running Kiota with Docker

1. Execute the following command to start generating SDKs

    ```shell
    docker run -v /some/output/path:/app/output \
    -v /some/input/description.yml:/app/openapi.yml \
    ghcr.io/microsoft/kiota/generator --language csharp -n samespaceprefix
    ```

    > **Note:** you can alternatively use the `--openapi` parameter with a URI instead of volume mapping.

    To generate a SDK from an online OpenAPI description and into the current directory:

    ```shell
    docker run -v ${PWD}:/app/output ghcr.io/microsoft/kiota/generator \
    --language typescript -n gfx -d \
    https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml
    ```
