# Building Kiota

1. Clone the current repository.
1. Install [the pre-requesites](./tool.md).
1. Open the solution with Visual Studio and right click *publish* **--or--** execute the following commands:

    ```Shell
    dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -r win-x64
    ```

1. Navigate to the output directory (usually under `src/kiota/bin/Release/net6.0`).
1. Run `kiota.exe ...`.

> Note: refer to [.NET runtime identifier catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) so select the appropriate runtime for your platform.
