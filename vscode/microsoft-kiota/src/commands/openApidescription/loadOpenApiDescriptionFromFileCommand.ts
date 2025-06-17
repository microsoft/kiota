import { getKiotaTree } from "@microsoft/kiota";
import { TelemetryReporter } from "@vscode/extension-telemetry";
import * as vscode from "vscode";
import * as fs from "fs";

import { extensionId, SHOW_MESSAGE_AFTER_API_LOAD, treeViewId } from "../../constants";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getExtensionSettings } from "../../types/extensionSettings";
import { updateTreeViewIcons } from "../../util";
import { openTreeViewWithProgress } from "../../utilities/progress";
import { Command } from "../Command";

export class LoadOpenApiDescriptionFromFileCommand extends Command {

  constructor(
    private openApiTreeProvider: OpenApiTreeProvider,
    private context: vscode.ExtensionContext
  ) {
    super();
  }

  public getName(): string {
    return `${treeViewId}.loadOpenApiDescriptionFromFile`;
  }

  public async execute(fileUri: vscode.Uri): Promise<void> {
    const reporter = new TelemetryReporter(this.context.extension.packageJSON.telemetryInstrumentationKey);
    
    try {
      // Get the file path
      const filePath = fileUri.fsPath;
      
      // Check if file exists and is readable
      if (!fs.existsSync(filePath)) {
        vscode.window.showErrorMessage(vscode.l10n.t("File not found: {0}", filePath));
        return;
      }

      // Check if it's a YAML file
      const fileExtension = filePath.toLowerCase();
      if (!fileExtension.endsWith('.yaml') && !fileExtension.endsWith('.yml')) {
        vscode.window.showErrorMessage(vscode.l10n.t("Selected file must be a YAML file (.yaml or .yml)"));
        return;
      }

      // Try to validate if it's an OpenAPI file by reading its content
      let fileContent: string;
      try {
        fileContent = fs.readFileSync(filePath, 'utf8');
      } catch (error) {
        vscode.window.showErrorMessage(vscode.l10n.t("Unable to read file: {0}", (error as Error).message));
        return;
      }

      // Basic check for OpenAPI/Swagger keywords
      const isOpenApiFile = this.isOpenApiContent(fileContent);
      if (!isOpenApiFile) {
        const loadAnyway = vscode.l10n.t("Load anyway");
        const response = await vscode.window.showWarningMessage(
          vscode.l10n.t("This file doesn't appear to be an OpenAPI description. It should contain 'openapi' or 'swagger' keywords."),
          loadAnyway,
          vscode.l10n.t("Cancel")
        );
        
        if (response !== loadAnyway) {
          return;
        }
      }

      // Check if there are changes that would be lost
      const yesAnswer = vscode.l10n.t("Yes, override it");
      if (this.openApiTreeProvider.hasChanges()) {
        const response = await vscode.window.showWarningMessage(
          vscode.l10n.t("Before adding a new API description, consider that your changes and current selection will be lost."),
          yesAnswer,
          vscode.l10n.t("Cancel")
        );
        if (response !== yesAnswer) {
          return;
        }
      }

      // Try to load the OpenAPI description
      const settings = getExtensionSettings(extensionId);
      
      // First validate the file using Kiota
      await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        cancellable: false,
        title: vscode.l10n.t("Validating OpenAPI description...")
      }, async (progress, _) => {
        try {
          await getKiotaTree({ 
            descriptionPath: filePath, 
            clearCache: settings.clearCache,
            includeKiotaValidationRules: true 
          });
        } catch (error) {
          throw new Error(vscode.l10n.t("Failed to validate OpenAPI description: {0}", (error as Error).message));
        }
      });

      // If validation passed, load it into the tree view
      await openTreeViewWithProgress(async () => {
        await this.openApiTreeProvider.setDescriptionUrl(filePath);
        await updateTreeViewIcons(treeViewId, true, false);
      });

      // Show success message and offer to generate
      const generateAnswer = vscode.l10n.t("Generate");
      const showGenerateMessage = this.context.globalState.get<boolean>(SHOW_MESSAGE_AFTER_API_LOAD, true);

      if (showGenerateMessage) {
        const doNotShowAgainOption = vscode.l10n.t("Do not show this again");
        const response = await vscode.window.showInformationMessage(
          vscode.l10n.t('OpenAPI description loaded successfully. Click on Generate after selecting the paths in the API Explorer'),
          generateAnswer,
          doNotShowAgainOption
        );

        if (response === generateAnswer) {
          await vscode.commands.executeCommand(`${treeViewId}.generateClient`);
        } else if (response === doNotShowAgainOption) {
          await this.context.globalState.update(SHOW_MESSAGE_AFTER_API_LOAD, false);
        }
      }

      // Send telemetry
      reporter.sendTelemetryEvent("LoadOpenApiDescriptionFromFile", {
        "fileExtension": fileExtension.endsWith('.yaml') ? 'yaml' : 'yml',
        "isDetectedAsOpenApi": isOpenApiFile.toString()
      });

    } catch (error) {
      const errorMessage = (error as Error).message;
      vscode.window.showErrorMessage(errorMessage);
      
      // Send error telemetry
      reporter.sendTelemetryEvent("LoadOpenApiDescriptionFromFile", {
        "error": errorMessage
      });
    }
  }

  private isOpenApiContent(content: string): boolean {
    // Convert to lowercase for case-insensitive matching
    const lowerContent = content.toLowerCase();
    
    // Look for OpenAPI or Swagger keywords
    return lowerContent.includes('openapi:') || 
           lowerContent.includes('swagger:') ||
           lowerContent.includes('"openapi"') ||
           lowerContent.includes('"swagger"') ||
           lowerContent.includes('openapi ') ||
           lowerContent.includes('swagger ');
  }
}