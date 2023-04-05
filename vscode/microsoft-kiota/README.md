# Microsoft Kiota

Kiota is a client generator for HTTP REST APIs described by OpenAPI. The experience is available as [a command-line tool](https://www.nuget.org/packages/Microsoft.OpenApi.Kiota) and as [a Visual Studio Code extension](https://aka.ms/kiota/extension).
Kiota helps eliminate the need to take a dependency on a different API client for every API that you need to call, as well as limiting the generation to the exact API surface area you’re interested in, thanks to a filtering capability.

## Features

Using kiota you can:

1. Search for API descriptions.
1. Filter and select the API endpoints you need.
1. Generate models and a chained method API surface in the language of your choice.
1. Call the API with the new client.

All that in a matter of seconds.

## Microsoft Kiota extension for Visual Studio Code 

This [Visual Studio Code](https://code.visualstudio.com/) (VS Code) extension adds a rich UI for the Kiota experience. The features include all of Kiota capabilities such as search for API descriptions, filtering and generating API clients and more! 

 * Once the extension is installed, you will be able to see the commands available to you. 

<img width="482" alt="VScode extension commands" src="https://user-images.githubusercontent.com/5781590/229946855-faff33bf-4e18-45eb-9b15-a42ac959a916.png">

* Search for an API description using a keyword 

<img width="479" alt="vscode entension search " src="https://user-images.githubusercontent.com/5781590/229947287-3a2850d0-d97e-4a1e-9440-9c97f8e66e1a.png">

<img width="478" alt="vscode extension search results" src="https://user-images.githubusercontent.com/5781590/229947317-dd24f722-d58c-41a6-a85b-fa7c0d48493e.png">

* Select the OpenAPI description you are interested in and you will be presented with the Kiota OpenAPI Explorer containing all the available endpoints 

<img width="189" alt="Kiota OpenAPI explorer" src="https://user-images.githubusercontent.com/5781590/229947806-27ff49b9-5877-41a2-b7df-c9c19f6f736e.png">

* Select the endpoints to include in your API client 

<img width="225" alt="kiota vscode select endpoint" src="https://user-images.githubusercontent.com/5781590/229948168-efecfd85-214a-4d65-a225-10b100b15a68.png">

* Finally, you can generate the API client. You will be prompted to provide some parameters for your client such as the class and namespace names. You will also need to select the language for the generated client.

<img width="349" alt="generation complete" src="https://user-images.githubusercontent.com/5781590/229949052-159f3a58-b0e6-421f-9ca5-b45dc49c4639.png">


## Requirements

None.

## Extension Settings

None.

## Known Issues

Checkout the [list of open issues](https://github.com/microsoft/kiota/issues) to get a list of the known issues.

## Release Notes

Checkout the [release notes](https://github.com/microsoft/kiota/releases) to get more information about each release.
