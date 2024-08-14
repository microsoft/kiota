import * as vscode from "vscode";
import { OpenApiTreeNode } from "../openApiTreeProvider";
import { Command } from "./Command";

export class OpenApiTreeNodeCommand extends Command {
  constructor() {
    super();
  }

  execute(openApiTreeNode: OpenApiTreeNode): void {
    if (openApiTreeNode.documentationUrl) {
      vscode.env.openExternal(vscode.Uri.parse(openApiTreeNode.documentationUrl));
    }
  }

}