import * as vscode from "vscode";
import { OpenApiTreeNode } from "../openApiTreeProvider";

export class OpenApiTreeNodeCommand {
  constructor() { }

  public openDocumentPage(openApiTreeNode: OpenApiTreeNode) {
    if (openApiTreeNode.documentationUrl) {
      vscode.env.openExternal(vscode.Uri.parse(openApiTreeNode.documentationUrl));
    }
  }
  
}