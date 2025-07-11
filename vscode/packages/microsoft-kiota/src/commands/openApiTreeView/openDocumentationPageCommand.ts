import * as vscode from "vscode";

import { treeViewId } from "../../constants";
import { OpenApiTreeNode } from "../../providers/openApiTreeProvider";
import { Command } from "../Command";

export class OpenDocumentationPageCommand extends Command {

  public getName(): string {
    return `${treeViewId}.openDocumentationPage`;
  }

  public async execute(node: OpenApiTreeNode): Promise<void> {
    node.documentationUrl && vscode.env.openExternal(vscode.Uri.parse(node.documentationUrl));
  }

}