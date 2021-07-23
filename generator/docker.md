# Running Kiota with Docker

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: read:package).
1. Copy the token, you will need it later.
1. Enable the SSO on the token if you are a Microsoft employee.
1. Execute the following command to login to the registry.

    ```Shell
    echo "<the personal access token>" | docker login "https://docker.pkg.github.com/microsoft/kiota/generator" -u "<your github username>" --password-stdin
    ```

1. Execute the following command to start generating SDKs

    ```Shell
    docker run -v /some/output/path:/app/output -v /some/input/description.yml:/app/openapi.yml docker.pkg.github.com/microsoft/kiota/generator --language csharp -n samespaceprefix
    ```

    > Note: you can alternatively use the --openapi parameter with a URI instead of volume mapping.

> Note: steps 1-4 only need to be done once per machine.
