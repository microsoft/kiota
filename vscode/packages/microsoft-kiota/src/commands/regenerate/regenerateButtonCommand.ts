import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, KIOTA_WORKSPACE_FILE, treeViewId } from "../../constants";
import { getGenerationConfiguration, setGenerationConfiguration } from "../../handlers/configurationHandler";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getExtensionSettings } from "../../types/extensionSettings";
import { WorkspaceGenerationContext } from "../../types/WorkspaceGenerationContext";
import { isClientType, isPluginType } from "../../util";
import { confirmDeletionOnCleanOutput } from "../../utilities/generation";
import { confirmOverwriteOnRegenerate } from "../../utilities/regeneration";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";

export class RegenerateButtonCommand extends Command {

  constructor(private _context: ExtensionContext, private _openApiTreeProvider: OpenApiTreeProvider, private _kiotaOutputChannel: vscode.LogOutputChannel) {
    super();
  }

  public getName(): string {
    return `${treeViewId}.regenerateButton`;
  }

  public async execute({ generationType, clientOrPluginKey, clientOrPluginObject }: WorkspaceGenerationContext): Promise<void> {
    const configuration = getGenerationConfiguration();
    const settings = getExtensionSettings(extensionId);
    if (settings.cleanOutput) {
      const accept = await confirmDeletionOnCleanOutput();
      if (!accept) {
        // cancel generation and open settings
        return vscode.commands.executeCommand('workbench.action.openSettings', 'kiota.cleanOutput.enabled');
      }
    } else if (!(await confirmOverwriteOnRegenerate())) {
      return;
    }

    if (!clientOrPluginKey || clientOrPluginKey === '') {
      clientOrPluginKey = configuration.clientClassName || configuration.pluginName || '';
    }

    if (!configuration) {
      setGenerationConfiguration({
        outputPath: clientOrPluginObject.outputPath,
        clientClassName: clientOrPluginKey,
      });
    }

    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }

    const configObject = clientOrPluginObject || configuration;
    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, clientOrPluginKey, configObject, this._kiotaOutputChannel);

    if (isClientType(generationType)) {
      await regenerateService.regenerateClient(settings, selectedPaths);
    }
    if (isPluginType(generationType)) {
      await regenerateService.regeneratePlugin(settings, selectedPaths);
      const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
      if (workspaceJson && !workspaceJson.isDirty) {
        await regenerateService.regenerateTeamsApp(workspaceJson, clientOrPluginKey);
      }
    }
    await vscode.commands.executeCommand('kiota.workspace.refresh');
  }

}