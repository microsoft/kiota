import * as vscode from "vscode";
import { Command } from "./Command";

export class KiotaStatusCommand extends Command {
  public constructor() {
    super();
  }

  async execute() {
    const yesAnswer = vscode.l10n.t("Yes");
    const response = await vscode.window.showInformationMessage(
      vscode.l10n.t("Open installation instructions for kiota?"),
      yesAnswer,
      vscode.l10n.t("No")
    );
    if (response === yesAnswer) {
      await vscode.env.openExternal(vscode.Uri.parse("https://aka.ms/get/kiota"));
    }
  }
}