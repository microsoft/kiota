import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { QuickPickItem } from "vscode";
import { APIMANIFEST, CLIENT, CLIENTS, KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE, PLUGIN, PLUGINS } from './constants';
import { GenerationType, KiotaGenerationLanguage, KiotaPluginType } from './enums';
import { allGenerationLanguages } from './kiotaInterop';
import { displayMigrationMessages, migrateFromLockFile } from './migrateFromLockFile';

const clientTypes = [CLIENT, CLIENTS];
const pluginTypes = [PLUGIN, PLUGINS, APIMANIFEST];

export function isClientType(type: string): boolean {
  return clientTypes.includes(type);
}

export function isPluginType(type: string): boolean {
  return pluginTypes.includes(type);
}

export async function updateTreeViewIcons(treeViewId: string, showIcons: boolean, showRegenerateIcon: boolean = false) {
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showIcons`, showIcons);
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showRegenerateIcon`, showRegenerateIcon);
}

export function getWorkspaceJsonPath(): string {
    return path.join(getWorkspaceJsonDirectory(),KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE);
};

export function getWorkspaceJsonDirectory(clientNameOrPluginName?: string): string {
  const baseDir = path.join(
    vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.workspace.workspaceFolders[0].uri.fsPath
      : process.env.HOME ?? process.env.USERPROFILE ?? process.cwd()
  );

  let workspaceFolder = !vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0
    ? path.join(baseDir, 'kiota', clientNameOrPluginName ?? '')
    : baseDir;

  if (!fs.existsSync(workspaceFolder)) {
    fs.mkdirSync(workspaceFolder, { recursive: true });
  }
  return workspaceFolder;
}

//used to store output in the App Package directory in the case where the workspace is a Teams Toolkit Project
export function findAppPackageDirectory(directory: string): string | null {
  if (!fs.existsSync(directory)) {
    return null;
  }

  const entries = fs.readdirSync(directory, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      if (entry.name === 'appPackage') {
        return fullPath;
      }

      const subDirectory = findAppPackageDirectory(fullPath);
      if (subDirectory) {
        return subDirectory;
      }
    }
  }

  return null;
}

export async function handleMigration(
  context: vscode.ExtensionContext,
  workspaceFolder: vscode.WorkspaceFolder
): Promise<void> {
  vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      title: vscode.l10n.t("Migrating your API clients..."),
      cancellable: false
  }, async (progress) => {
      progress.report({ increment: 0 });

      try {
          const migrationResult = await migrateFromLockFile(context, workspaceFolder.uri.fsPath);

          progress.report({ increment: 100 });

          if (migrationResult && migrationResult.length > 0) {
              displayMigrationMessages(migrationResult);
          } else {
              vscode.window.showWarningMessage(vscode.l10n.t("Migration completed, but no changes were detected."));
          }
      } catch (error) {
          vscode.window.showErrorMessage(vscode.l10n.t(`Migration failed: ${error}`));
      }
  });
}

export function getSanitizedString(rawValue?: string): string| undefined{
  return rawValue?.replace(/[^a-zA-Z0-9_]+/g, '');
};

export function parseGenerationType(generationType: string | QuickPickItem | undefined): GenerationType {
    if(typeof generationType !== 'string') {
        throw new Error('generationType has not been selected yet');
    }
    switch(generationType) {
        case "client":
            return GenerationType.Client;
        case "plugin":
            return GenerationType.Plugin;
        case "apimanifest":
            return GenerationType.ApiManifest;
        default:
            throw new Error(`Unknown generation type ${generationType}`);
    }
}

export function parseGenerationLanguage(value: string): KiotaGenerationLanguage {
    switch (value.toLowerCase()) {
      case "csharp":
          return KiotaGenerationLanguage.CSharp;
      case "java":
          return KiotaGenerationLanguage.Java;
      case "typescript":
          return KiotaGenerationLanguage.TypeScript;
      case "php":
          return KiotaGenerationLanguage.PHP;
      case "python":
          return KiotaGenerationLanguage.Python;
      case "go":
          return KiotaGenerationLanguage.Go;
      case "swift":
          return KiotaGenerationLanguage.Swift;
      case "ruby":
          return KiotaGenerationLanguage.Ruby;
      case "cli":
          return KiotaGenerationLanguage.CLI;
      default:
          throw new Error("unknown language " + value);
    }
}

export function parsePluginType(values: string[]): KiotaPluginType[] {
    return values.map(value => {
        switch (value.toLowerCase()) {
            case "openai":
                return KiotaPluginType.OpenAI;
            case "apimanifest":
                return KiotaPluginType.ApiManifest;
            case "apiplugin":
                return KiotaPluginType.ApiPlugin;
            default:
                throw new Error(`unknown plugin type: ${value}`);
        }
    });
}

export function allGenerationLanguagesToString(): string[] {
  let allSupportedLanguages: string[] = allGenerationLanguages.map(langEnum => KiotaGenerationLanguage[langEnum]);
  return allSupportedLanguages;
}

export function validateDeepLinkQueryParams(queryParameters: Record<string, string>):
 [Record<string, string|undefined>, string[]]
{
  let errormsg: string [] = [];
  let validQueryParams: Record<string, string|undefined> = {};
  const descriptionUrl = queryParameters["descriptionurl"];
  const name = getSanitizedString(queryParameters["name"]);
  const source = getSanitizedString(queryParameters["source"]);
  let lowercasedKind: string = queryParameters["kind"]?.toLowerCase();
  let validKind: string | undefined = ["plugin", "client"].indexOf(lowercasedKind) > -1 ? lowercasedKind : undefined ;
  if (!validKind){
    errormsg.push(
      "Invalid parameter 'kind' deeplinked. Actual value: " + lowercasedKind + 
      "Expected values: 'plugin' or 'client'"
    );
  }
  let givenLanguage: string | undefined = undefined;
  try{
    if (queryParameters["language"]){
      let languageEnumerator = parseGenerationLanguage(queryParameters["language"]);
      givenLanguage = KiotaGenerationLanguage[languageEnumerator];
    }
  }catch (e){
    if (e instanceof Error) {
        errormsg.push(e.message);
    } else {
        errormsg.push(String(e));
    }

  }
  if (!givenLanguage && validKind === "client"){
    let acceptedLanguages: string [] = allGenerationLanguagesToString();
    errormsg.push("Invalid 'language'= " + queryParameters["language"] + " parameter deeplinked. Supported languages are : " + acceptedLanguages.join(","));
  }
  let providedType: string | undefined =  undefined;
  try{
    if (queryParameters["type"]){
      let pluginTypeEnumerator : KiotaPluginType = parsePluginType([queryParameters["type"]])[0];
      providedType = KiotaPluginType[pluginTypeEnumerator]?.toLowerCase();
    }
  }catch(e){
    if (e instanceof Error) {
        errormsg.push(e.message);
    } else {
        errormsg.push(String(e));
    }
  }
  if (!providedType && validKind === "plugin"){
    let acceptedPluginTypes: string [] = Object.keys(KiotaPluginType).filter(x => !Number(x) && x !=='0').map(x => x.toString().toLowerCase());
    errormsg.push("Invalid parameter 'type' deeplinked. Expected values: " + acceptedPluginTypes.join(","));
  }

  validQueryParams = {
    descriptionUrl: descriptionUrl,
    name: name,
    kind: validKind,
    type: providedType,
    language: givenLanguage,
    source: source,
  };
  return [validQueryParams, errormsg];
}
