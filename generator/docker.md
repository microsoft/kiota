# Running Kiota with Docker

1. Execute the following command to start generating SDKs

    ```Shell
    docker run -v /some/output/path:/app/output -v /some/input/description.yml:/app/openapi.yml ghcr.io/microsoft/kiota/generator --language csharp -n samespaceprefix
    ```

    > Note: you can alternatively use the --openapi parameter with a URI instead of volume mapping.

> Note: steps 1-4 only need to be done once per machine.
