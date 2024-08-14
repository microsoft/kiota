import * as vscode from "vscode";

import { treeViewId } from "../../constants";
import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { GenerateState } from "../../steps";
import { getWorkspaceJsonPath, updateTreeViewIcons } from "../../util";
import { loadLockFile } from "../../utilities/file";

async function displayGenerationResults(config: Partial<GenerateState>, _outputPath: string, openApiTreeProvider: OpenApiTreeProvider) {
  const clientNameOrPluginName = config.clientClassName || config.pluginName;
  openApiTreeProvider.refreshView();
  const workspaceJsonPath = getWorkspaceJsonPath();
  await loadLockFile({ fsPath: workspaceJsonPath }, openApiTreeProvider, clientNameOrPluginName);
  await vscode.commands.executeCommand('kiota.workspace.refresh');
  openApiTreeProvider.resetInitialState();
  await updateTreeViewIcons(treeViewId, false, true);
}

export {
  displayGenerationResults
};

