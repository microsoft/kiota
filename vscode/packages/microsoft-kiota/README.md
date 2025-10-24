# Kiota

Kiota is a client/plugin/manifest generator for HTTP REST APIs described by OpenAPI. The experience is available as [a command-line tool](https://www.nuget.org/packages/Microsoft.OpenApi.Kiota) and as [a Visual Studio Code extension](https://aka.ms/kiota/extension).
Kiota helps eliminate the need to take a dependency on a different API client for every API that you need to call, as well as limiting the generation to the exact API surface area you’re interested in, thanks to a filtering capability. It also helps with participating in the Microsoft Copilot ecosystem by enabling generation of API plugins.

## Capabilities

Using kiota you can:

1. Search for API descriptions.
2. Filter and select the API endpoints you need by slicing only required endpoints from a rather bulky OpenAPI Description
3. Generate models and a chained method API surface in the language of your choice. Supported languages can be viewed at <https://github.com/microsoft/kiota/tree/main?tab=readme-ov-file#supported-languages>
4. Call the OpenAPI described API with the new client generated in step 3 above.
5. Generate API plugin manifests that can be easily integrated into Microsoft Copilot. **New**
6. Generate [API manifests](https://datatracker.ietf.org/doc/draft-miller-api-manifest/). **New**

All that in a matter of seconds.

## Kiota extension for Visual Studio Code

This [Visual Studio Code](https://code.visualstudio.com/) (VS Code) extension adds a rich UI for the Kiota experience. The features include all of Kiota capabilities such as search for API descriptions, filtering and generating API clients and more!

## Kiota extension installation

1. In VS Code, navigate to `Extensions`.
<img width="482" alt="Navigate to Extensions on VS Code" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/Navigate%20to%20Extensions%20on%20VS%20Code.png">

2. Search for 'kiota'
3. Click on Install.

You can also install the extension package from the [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=ms-graph.kiota).

## Getting started

Once the extension is installed, you will be able to see the commands available to you.

You can kick start the process by using the add file icons as appears below or using command pallete with the command "Add API description"

<img width="482" alt="Use the add file icon" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/SearchOrBrowseOptions.png">

The notification bar will also notify you of ongoing background processes e.g when searching for an api using a keyword

<img width="482" alt="vscode extension search notification" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/searchingNotification.png">

The search results will be displayed as below once the search is complete

<img width="482" alt="vscode extension search results" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/searchResults.png">

Select the OpenAPI description you are interested in and you will be presented with the Kiota OpenAPI Explorer containing all the available endpoints

<img width="482" alt="Kiota OpenAPI explorer" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/endpointSelectionandTheGenerateIcon.png">

Select the endpoints to include in your API client as above and click the `generate` icon. Kiota extension will display with the options to generate either client, plugin or other.

<img width="482" alt="kiota vscode generate options" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/SelectGenerationOption.png">

Finally, the notification bar at the bottom right will display "Generation Completed Successfully" once the generation is done. Click on your vscode file explorer to see the generated outputs in the current workspace if default selection was your output directory choice or navigate to selected folder to see the output.

## Migrating from older use of lock file to a workspace.json

The latest version of the Kiota extension has been improved to using a workspace instead of a lock file to facilitate having multiple clients, plugins, or manifests within the same workspace. The workspace.json file also provides an opportunity to edit, or regenerate the outputs conveniently.

<img width="482" alt="Working with the extension's workspace" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/GenerationMultipleClientsSameWorkspace.png">

If you previously generated a client that had a lock file, kiota prompts you to migrate to using the workspace once you open the previously generated folder containing a lock file. You can choose to migrate immediately or ask to be reminded later.

<img width="482" alt="Notification to Migrate to extension's workspace" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages/microsoft-kiota/images/samples/MigratePromptOnOpeningFolder.png">

You can also access the same feature later by using the contextual option on the lock file, by right-clicking on the lock file and selecting the option to migrate.

<img width="482" alt="Contextual Migrate option on right-clicking on lockfile" src="https://raw.githubusercontent.com/microsoft/kiota/main/vscode/packages//microsoft-kiota/images/samples/ContextualMigrateLockToWorkspace.png">

Enjoy the benefits of the workspace once the migration is complete.

## Extension Settings

1. Navigate to extensions using the UI or (ctrl+shift+x) or (cmd+shift+x) for mac users
2. Search 'kiota'
3. Click on the settings icon on the kiota extension
4. On the dropdown navigate to `Extension Settings` and click on it.
5. This should open a settings tab on your vscode that has filtered for all Kiota supported extensions.
6. Feel free to leave the settings as is or customize them for even better control.

## Contributions

There are many ways in which you can participate in the project, for example:

- [Download our latest builds](https://github.com/microsoft/kiota/releases).
- [Submit bugs and feature requests](https://github.com/microsoft/kiota/issues), and help us verify as they are checked in
- Review [source code changes](https://github.com/microsoft/kiota/pulls)
- Review the [documentation](https://github.com/microsoft/kiota/blob/main/vscode/packages/microsoft-kiota/README.md) and make pull requests for anything from typos to new content

See our contributions guidelines in the [CONTRIBUTING.md](https://github.com/microsoft/kiota/blob/main/vscode/packages/microsoft-kiota/CONTRIBUTING.md) page for more information.
Further guidelines are also available in the page [SUPPORT.md](https://github.com/microsoft/kiota/blob/main/SUPPORT.md)

## Reporting security issues

Check out our [SECURITY.md](https://github.com/microsoft/kiota/blob/main/SECURITY.md) page for details.

## Telemetry

Kiota extension collects usage data and sends it to Microsoft to help improve our products and services. Read our [Privacy Statement](https://go.microsoft.com/fwlink/?LinkId=521839) and [Data Collection Notice](https://docs.opensource.microsoft.com/content/releasing/telemetry.html) to learn more. Learn more in our [FAQ](https://code.visualstudio.com/docs/supporting/faq#_how-to-disable-telemetry-reporting).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
