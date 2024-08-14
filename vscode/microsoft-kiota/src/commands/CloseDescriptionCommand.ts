import * as vscode from "vscode";

import { treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { updateTreeViewIcons } from "../util";
import { Command } from "./Command";

export class CloseDescriptionCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;

  public constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public toString(): string {
    return `${treeViewId}.closeDescription`;
  }

  async execute(): Promise<void> {
    const yesAnswer = vscode.l10n.t("Yes");
    const response = await vscode.window.showInformationMessage(
      vscode.l10n.t("Do you want to remove this API description?"),
      yesAnswer,
      vscode.l10n.t("No")
    );
    if (response === yesAnswer) {
      this._openApiTreeProvider.closeDescription();
      await updateTreeViewIcons(treeViewId, false);
    }
}  
}