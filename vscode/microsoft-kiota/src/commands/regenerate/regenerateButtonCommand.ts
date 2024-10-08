import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, treeViewId } from "../../constants";
import { getExtensionSettings } from "../../extensionSettings";
import { getGenerationConfiguration, setGenerationConfiguration } from "../../handlers/configurationHandler";
import { getWorkspaceGenerationType } from "../../handlers/workspaceGenerationTypeHandler";
import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { isClientType, isPluginType } from "../../util";
import { confirmOverride } from "../../utilities/regeneration";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";
import { ClientOrPluginProperties } from "../../kiotaInterop";

export class RegenerateButtonCommand extends Command {
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${treeViewId}.regenerateButton`;
  }

  public async execute({ clientOrPluginKey, clientOrPluginObject }: { clientOrPluginKey: string; clientOrPluginObject: ClientOrPluginProperties }): Promise<void> {
    const configuration = getGenerationConfiguration();
    const regenerate = await confirmOverride();
    if (!regenerate) {
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

    const settings = getExtensionSettings(extensionId);
    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }
    const workspaceGenerationType = getWorkspaceGenerationType();
    const configObject = clientOrPluginObject || configuration;
    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, clientOrPluginKey, configObject);

    if (isClientType(workspaceGenerationType)) {
      await regenerateService.regenerateClient(settings, selectedPaths);
    }
    if (isPluginType(workspaceGenerationType)) {
      await regenerateService.regeneratePlugin(settings, selectedPaths);
    }
  }

}