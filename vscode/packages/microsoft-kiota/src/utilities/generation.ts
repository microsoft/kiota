import {l10n, window} from "vscode";


export async function confirmDeletionOnCleanOutput(): Promise<boolean> {
  const yesAnswer = l10n.t("Yes, proceed");
  const noAnswer = l10n.t("Change configuration");
  const message = l10n.t("All existing files in the output directory are going to be deleted.");
  const confirmation = await window.showWarningMessage(
    message, yesAnswer, noAnswer
  );
  return confirmation === yesAnswer;
}
