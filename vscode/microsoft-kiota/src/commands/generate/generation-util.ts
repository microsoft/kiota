import * as vscode from "vscode";

import { treeViewId } from "../../constants";
import { KiotaLogEntry } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { getWorkspaceJsonPath, updateTreeViewIcons } from "../../util";
import { loadWorkspaceFile } from "../../utilities/file";

export async function checkForSuccess(results: KiotaLogEntry[]) {
  for (const result of results) {
    if (result && result.message) {
      if (result.message.includes("Generation completed successfully")) {
        return true;
      }
    }
  }
  return false;
}

export async function displayGenerationResults(openApiTreeProvider: OpenApiTreeProvider, config: any) {
  const clientNameOrPluginName = config.clientClassName || config.pluginName;
  openApiTreeProvider.refreshView();
  const workspaceJsonPath = getWorkspaceJsonPath();
  await loadWorkspaceFile({ fsPath: workspaceJsonPath }, openApiTreeProvider, clientNameOrPluginName);
  await vscode.commands.executeCommand('kiota.workspace.refresh');
  openApiTreeProvider.resetInitialState();
  await updateTreeViewIcons(treeViewId, false, true);
}