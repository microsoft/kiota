import TelemetryReporter from "@vscode/extension-telemetry";
import * as vscode from "vscode";

import { extensionId, SHOW_MESSAGE_AFTER_API_LOAD, treeViewId } from "../../constants";
import { setDeepLinkParams } from "../../handlers/deepLinkParamsHandler";
import { searchDescription } from "../../kiotaInterop";
import { searchSteps } from "../../modules/steps/searchSteps";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getExtensionSettings } from "../../types/extensionSettings";
import { updateTreeViewIcons } from "../../util";
import { IntegrationParams, validateDeepLinkQueryParams } from "../../utilities/deep-linking";
import { openTreeViewWithProgress } from "../../utilities/progress";
import { Command } from "../Command";

export class SearchOrOpenApiDescriptionCommand extends Command {

  constructor(
    private openApiTreeProvider: OpenApiTreeProvider,
    private context: vscode.ExtensionContext
  ) {
    super();
  }

  public getName(): string {
    return `${treeViewId}.searchOrOpenApiDescription`;
  }

  public async execute(searchParams: Partial<IntegrationParams>): Promise<void> {
    // set deeplink params if exists
    if (Object.keys(searchParams).length > 0) {
      let [params, errorsArray] = validateDeepLinkQueryParams(searchParams);
      setDeepLinkParams(params);
      const reporter = new TelemetryReporter(this.context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendTelemetryEvent("DeepLinked searchOrOpenApiDescription", {
        "searchParameters": JSON.stringify(searchParams),
        "validationErrors": errorsArray.join(", ")
      });
    }

    // proceed to enable loading of openapi description
    const yesAnswer = vscode.l10n.t("Yes, override it");
    if (this.openApiTreeProvider.hasChanges()) {
      const response = await vscode.window.showWarningMessage(
        vscode.l10n.t(
          "Before adding a new API description, consider that your changes and current selection will be lost."),
        yesAnswer,
        vscode.l10n.t("Cancel")
      );
      if (response !== yesAnswer) {
        return;
      }
    }

    const config = await searchSteps(x => vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Searching...")
    }, (progress, _) => {
      const settings = getExtensionSettings(extensionId);
      return searchDescription({ searchTerm: x, clearCache: settings.clearCache });
    }));

    if (config.descriptionPath) {
      try {
        await openTreeViewWithProgress(async () => {
          await this.openApiTreeProvider.setDescriptionUrl(config.descriptionPath!);
          await updateTreeViewIcons(treeViewId, true, false);
        });
      } catch (err) {
        const error = err as Error;
        vscode.window.showErrorMessage(error.message);
        return;
      }

      const generateAnswer = vscode.l10n.t("Generate");
      const showGenerateMessage = this.context.globalState.get<boolean>(SHOW_MESSAGE_AFTER_API_LOAD, true);

      if (showGenerateMessage) {
        const doNotShowAgainOption = vscode.l10n.t("Do not show this again");
        const response = await vscode.window.showInformationMessage(
          vscode.l10n.t('Click on Generate after selecting the paths in the API Explorer'),
          generateAnswer,
          doNotShowAgainOption
        );

        if (response === generateAnswer) {
          await vscode.commands.executeCommand(`${treeViewId}.generateClient`);
        } else if (response === doNotShowAgainOption) {
          await this.context.globalState.update(SHOW_MESSAGE_AFTER_API_LOAD, false);
        }
      }
    }
  }
}
