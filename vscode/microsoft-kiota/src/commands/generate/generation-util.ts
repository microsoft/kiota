import * as vscode from "vscode";

import { treeViewId } from "../../constants";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getWorkspaceJsonPath, updateTreeViewIcons } from "../../util";
import { loadWorkspaceFile } from "../../utilities/file";

export async function displayGenerationResults(openApiTreeProvider: OpenApiTreeProvider, config: any) {
  const clientNameOrPluginName = config.clientClassName || config.pluginName;
  openApiTreeProvider.refreshView();
  const workspaceJsonPath = getWorkspaceJsonPath();
  await loadWorkspaceFile({ fsPath: workspaceJsonPath }, openApiTreeProvider, clientNameOrPluginName);
  await vscode.commands.executeCommand('kiota.workspace.refresh');
  openApiTreeProvider.resetInitialState();
  await updateTreeViewIcons(treeViewId, false, true);
}