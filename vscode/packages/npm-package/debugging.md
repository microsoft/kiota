# Debugging the package

Automated testing is the preferred method for ensuring the functionality of the Kiota RPC process. In order to debug the package, the simplest way is to run the tests in debug mode as defined in **Step 2** and that would be enough.

However, there may be instances where you need to debug the Kiota RPC process, especially when invoked from the npm package. This document outlines the steps to debug the full Kiota process.

>The Kiota RPC process is a .NET process that runs in the background and is invoked by the npm package. It is responsible for generating code based on the OpenAPI specification provided by the user.


## 1. Publish in Debug Mode

First, publish the Kiota project in **debug** mode to a target location.
>**Note:** The `-c Debug` flag is crucial as it ensures that the Kiota process is built in debug mode, allowing you to attach a debugger to it later.

The following example uses Windows and version `1.25.1` which is the latest version at the time of writing. You can change the version number as needed based on the version in the `package.json` file.

```sh
dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -f net10.0 -c Debug -r win-x64 -o ./vscode/packages/npm-package/.kiotabin/1.25.1/win-x64/
```

This command will create a folder in `vscode/packages/npm-package/.kiotabin/1.25.1/win-x64/` with the Kiota executable that will be used for the integration tests.

## 2. Run the Npm Package

Run the npm package to start the Kiota process. You can do this, for example, by running an integration test with a breakpoint set after the process is created but before the remote method is invoked.

e.g. add a breakpoint in the `vscode/packages/npm-package/lib/generatePlugin.ts` file, in the `generatePlugin` function, right after the first variable assignment.

>**Note:** The Kiota process is started by the npm package, and it will run in the background. You need to ensure that the process is running before you can attach the debugger to it.
Once the breakpoint is hit, you can proceed to the next step.

## 3. Attach to the Process


This step is crucial:

- Open your Visual Studio.
- Go to the **Debug** menu and select **Attach to Process...**.
- In the **Attach to Process** dialog, you will see a list of running processes.
- Look for the Kiota process. It should be named `kiota.exe`.
- If you don't see it, make sure to check the **Show processes from all users** checkbox.
- You can also use the **Find** feature to search for `kiota.exe`.
- Select the Kiota process from the list.
- Click the **Attach** button to attach the debugger to the Kiota process.

>**Important:** When attaching, select **"Managed (.NET Core, .NET 5+)"** as the code type, instead of the default automatic option. This ensures the debugger attaches correctly to the .NET process.



---

By following these steps, you should be able to debug the Kiota process started by the npm package.
