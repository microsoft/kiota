import * as vscode from "vscode";

export async function confirmOverwriteOnRegenerate(): Promise<boolean> {
  const yesAnswer = vscode.l10n.t("Yes, override it");
  const confirmation = await vscode.window
    .showWarningMessage(
      vscode.l10n.t("When regenerating, all changes made manually to the generated files will be overridden."),
      yesAnswer,
      vscode.l10n.t("Cancel")
    );
  return confirmation === yesAnswer;
}
