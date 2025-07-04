import { treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "./progress";

export async function loadWorkspaceFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider, clientOrPluginName?: string): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadWorkspaceFile(node.fsPath, clientOrPluginName));
  await updateTreeViewIcons(treeViewId, true);
}