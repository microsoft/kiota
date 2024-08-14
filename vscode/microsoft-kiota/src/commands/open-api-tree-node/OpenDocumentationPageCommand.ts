import * as vscode from "vscode";
import { OpenApiTreeNode } from "../../providers/openApiTreeProvider";
import { Command } from "../Command";

export class OpenDocumentationPageCommand extends Command {
  constructor() {
    super();
  }

  execute(openApiTreeNode: OpenApiTreeNode): void {
    if (openApiTreeNode.documentationUrl) {
      vscode.env.openExternal(vscode.Uri.parse(openApiTreeNode.documentationUrl));
    }
  }

}