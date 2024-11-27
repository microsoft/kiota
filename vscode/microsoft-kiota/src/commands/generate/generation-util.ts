import * as vscode from "vscode";

import { KIOTA_WORKSPACE_FILE, treeViewId } from "../../constants";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { updateTreeViewIcons } from "../../util";

export async function displayGenerationResults(openApiTreeProvider: OpenApiTreeProvider, config: any) {
  const clientNameOrPluginName = config.clientClassName || config.pluginName;
  const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
  if (workspaceJson) {
    const content = workspaceJson.getText();
    const workspace = JSON.parse(content);
    const clientOrPluginObject = workspace.plugins[clientNameOrPluginName] || workspace.clients[clientNameOrPluginName];
    await openApiTreeProvider.loadEditPaths(clientNameOrPluginName, clientOrPluginObject);
  }
  openApiTreeProvider.resetInitialState();
  await updateTreeViewIcons(treeViewId, false, true);
  await vscode.commands.executeCommand('kiota.workspace.refresh');
}