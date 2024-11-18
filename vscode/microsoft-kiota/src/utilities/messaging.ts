import * as vscode from "vscode";
import { MANIFEST_KIOTA_VERSION_KEY } from "../constants";

function checkVersion(clientVersion: string | null | undefined, packageKiotaVersion: any): Thenable<string | undefined> {
  if (clientVersion && clientVersion !== packageKiotaVersion) {
    return vscode.window.showWarningMessage(vscode.l10n.t("Client will be upgraded from version {0} to {1}, upgrade your dependencies", clientVersion, packageKiotaVersion));
  }
  return Promise.resolve(undefined);
}

type ApiDependencyPartial = { extensions: { [MANIFEST_KIOTA_VERSION_KEY]: string | undefined } | undefined };
type ManifestFilePartial = { apiDependencies: { [name: string]: ApiDependencyPartial | undefined } | undefined };

function extractClientVersion(data: ApiDependencyPartial): string | undefined {
  return data?.extensions?.[MANIFEST_KIOTA_VERSION_KEY];
}

export async function showUpgradeWarningMessage(apiManifestPath: string | vscode.Uri, manifestKey: string | null | undefined, generationType: string | null, context: vscode.ExtensionContext): Promise<void> {
  const kiotaVersion = context.extension.packageJSON.kiotaVersion;
  let manifestFileData;
  let manifestPathUri: vscode.Uri = apiManifestPath instanceof vscode.Uri ? apiManifestPath : vscode.Uri.file(apiManifestPath);
  try {
    manifestFileData = await vscode.workspace.fs.readFile(manifestPathUri);
  } catch (error) {
    // intentionally ignored
    return;
  }
  const manifestFile = JSON.parse(manifestFileData.toString()) as ManifestFilePartial;
  if (manifestKey) {
    const clientVersion = manifestFile?.apiDependencies?.[manifestKey]?.extensions?.[MANIFEST_KIOTA_VERSION_KEY];
    // don't fail if kiotaVersion isn't in the api manifest file
    if (clientVersion && clientVersion !== kiotaVersion) {
      // client is the default
      const elementCategory = generationType === "plugin" ? "plugin" : "client";
      // TODO: Add translations
      await vscode.window.showWarningMessage(vscode.l10n.t("The kiota version for {0} '{1}' will be changed from {2} to {3}, update your dependencies", elementCategory, manifestKey, clientVersion, kiotaVersion));
    }
  } else {
    // check if any versions will be changed. Ensures the message is displayed once for the update command
    let hasUpdate = false;
    for (const key in manifestFile?.apiDependencies) {
      const clientVersion = manifestFile?.apiDependencies?.[key]?.extensions?.[MANIFEST_KIOTA_VERSION_KEY];
      if (clientVersion && clientVersion !== kiotaVersion) {
        hasUpdate = true;
        break;
      }
    }
    if (hasUpdate) {
      // TODO: Add translations
      await vscode.window.showWarningMessage(vscode.l10n.t("The kiota version for all clients and plugins will be changed to {0}, update your dependencies", kiotaVersion));
    }
  }
}