import * as fs from 'fs';
import * as vscode from "vscode";

import { KIOTA_WORKSPACE_FILE, treeViewId } from "../../constants";
import { getLanguageInformationInternal, LanguagesInformation } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getWorkspaceJsonPath, updateTreeViewIcons } from "../../util";

let _languageInformation: LanguagesInformation | undefined; // doesn't change over the lifecycle of the extension

export async function displayGenerationResults(openApiTreeProvider: OpenApiTreeProvider, config: any) {
  const clientNameOrPluginName = config.clientClassName || config.pluginName;
  const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
  if (workspaceJson) {
    const content = await fs.promises.readFile(getWorkspaceJsonPath(), 'utf-8');
    const workspace = JSON.parse(content);
    const clientOrPluginObject = workspace.plugins[clientNameOrPluginName] || workspace.clients[clientNameOrPluginName];
    if (clientOrPluginObject) {
      await openApiTreeProvider.loadEditPaths(clientNameOrPluginName, clientOrPluginObject);
    }
  }
  openApiTreeProvider.resetInitialState();
  await updateTreeViewIcons(treeViewId, false, true);
  await vscode.commands.executeCommand('kiota.workspace.refresh');
}

export async function getLanguageInformation(): Promise<LanguagesInformation | undefined> {
  if (_languageInformation) {
    return _languageInformation;
  }
  const result = await getLanguageInformationInternal();
  if (result) {
    _languageInformation = result;
  }
  return result;
};