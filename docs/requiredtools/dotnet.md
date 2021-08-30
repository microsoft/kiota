# Required tools for Dotnet

- [.NET SDK 5.0](https://dotnet.microsoft.com/download)

## Initializing target projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](../abstractions/dotnet) and the [authentication](../authentication/dotnet/azure), [http](../http/dotnet/httpclient), [serialization](../serialization/dotnet/json) packages from the GitHub feed.

Execute the following command in the directory you want to initialize the project in.

```Shell
dotnet new console
dotnet new gitignore
```
