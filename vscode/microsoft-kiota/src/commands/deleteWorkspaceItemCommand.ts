import * as vscode from "vscode";
import * as rpc from "vscode-jsonrpc/node";

import { extensionId } from "../constants";
import { WorkspaceTreeItem } from "../providers/workspaceTreeProvider";
import { Command } from "./Command";
import { connectToKiota, KiotaLogEntry } from "../kiotaInterop";
import { isPluginType } from "../util";

export class DeleteWorkspaceItemCommand extends Command {
  constructor(private _context: vscode.ExtensionContext) {
    super();
  }

  public getName(): string {
    return `${extensionId}.workspace.deleteItem`;
  }

  public async execute(workspaceTreeItem: WorkspaceTreeItem): Promise<void> {
    const result = await deleteItem(this._context, isPluginType(workspaceTreeItem.category!) ? "plugin" : "client", workspaceTreeItem.label);
    vscode.window.showInformationMessage(`Delete item: ${workspaceTreeItem.label}`);
  }
}

export function deleteItem(context: vscode.ExtensionContext, workspaceItemType: string, key: string): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(context, async (connection) => {
    const request = new rpc.RequestType1<string, KiotaLogEntry[], void>(
      workspaceItemType
    );
    const result = await connection.sendRequest(
      request,
      key
    );
    return result;
  });
};