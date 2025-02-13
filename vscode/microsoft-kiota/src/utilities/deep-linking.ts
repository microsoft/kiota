import * as path from 'path';
import { promises as fs } from 'fs';

import { GenerateState } from "../modules/steps/generateSteps";
import { allGenerationLanguagesToString, getSanitizedString, parseGenerationLanguage, parsePluginType } from "../util";
import { createTemporaryFolder } from "./temporary-folder";
import { KiotaGenerationLanguage, KiotaPluginType } from '../kiotaInterop';

export function isDeeplinkEnabled(deepLinkParams: Partial<IntegrationParams>): boolean {
  const minimumNumberOfParams = 1;
  return Object.values(deepLinkParams).filter(property => property).length >= minimumNumberOfParams;
}

export async function transformToGenerationConfig(deepLinkParams: Partial<IntegrationParams>)
  : Promise<Partial<GenerateState>> {
  const generationConfig: Partial<GenerateState> = {};
  if (deepLinkParams.kind === "client") {
    generationConfig.generationType = "client";
    generationConfig.clientClassName = deepLinkParams.name;
    generationConfig.language = deepLinkParams.language;
  }
  else if (deepLinkParams.kind === "plugin") {
    generationConfig.pluginName = deepLinkParams.name;
    switch (deepLinkParams.type) {
      case "apiplugin":
        generationConfig.generationType = "plugin";
        generationConfig.pluginTypes = ["ApiPlugin"];
        break;
      case "openai":
        generationConfig.generationType = "other";
        generationConfig.pluginTypes = ['OpenAI'];
        break;
      case "apimanifest":
        generationConfig.generationType = "other";
        generationConfig.pluginTypes = ["ApiManifest"];
        break;
    }
    generationConfig.outputPath =
      (deepLinkParams.source && deepLinkParams.source?.toLowerCase() === 'ttk')
      ? await determineOutputPath(deepLinkParams)
        : undefined;
  }
  return generationConfig;
}

export interface IntegrationParams {
  descriptionurl: string;
  name: string;
  kind: string;
  type: string;
  language: string;
  source: string;
  ttkContext: {
    lastCommand: string;
  },
  projectPath: string;
};

export function validateDeepLinkQueryParams(queryParameters: Partial<IntegrationParams>):
  [Partial<IntegrationParams>, string[]] {
  let errormsg: string[] = [];
  let validQueryParams: Partial<IntegrationParams> = {};
  const descriptionurl = queryParameters["descriptionurl"];
  const name = getSanitizedString(queryParameters["name"]);
  const source = getSanitizedString(queryParameters["source"]);
  let projectPath = queryParameters["projectPath"];

  let lowercasedKind: string = queryParameters["kind"]?.toLowerCase() ?? "";
  let validKind: string | undefined = ["plugin", "client"].indexOf(lowercasedKind) > -1 ? lowercasedKind : undefined;
  if (!validKind) {
    errormsg.push(
      "Invalid parameter 'kind' deeplinked. Actual value: " + lowercasedKind +
      "Expected values: 'plugin' or 'client'"
    );
  }
  let givenLanguage: string | undefined = undefined;
  try {
    if (queryParameters["language"]) {
      let languageEnumerator = parseGenerationLanguage(queryParameters["language"]);
      givenLanguage = KiotaGenerationLanguage[languageEnumerator];
    }
  } catch (e) {
    if (e instanceof Error) {
      errormsg.push(e.message);
    } else {
      errormsg.push(String(e));
    }

  }
  if (!givenLanguage && validKind === "client") {
    let acceptedLanguages: string[] = allGenerationLanguagesToString();
    errormsg.push("Invalid 'language'= " + queryParameters["language"] + " parameter deeplinked. Supported languages are : " + acceptedLanguages.join(","));
  }
  let providedType: string | undefined = undefined;
  try {
    if (queryParameters["type"]) {
      let pluginTypeEnumerator: KiotaPluginType = parsePluginType([queryParameters["type"]])[0];
      providedType = KiotaPluginType[pluginTypeEnumerator]?.toLowerCase();
    }
  } catch (e) {
    if (e instanceof Error) {
      errormsg.push(e.message);
    } else {
      errormsg.push(String(e));
    }
  }
  if (!providedType && validKind === "plugin") {
    let acceptedPluginTypes: string[] = Object.keys(KiotaPluginType).filter(x => !Number(x) && x !== '0').map(x => x.toString().toLowerCase());
    errormsg.push("Invalid parameter 'type' deeplinked. Expected values: " + acceptedPluginTypes.join(","));
  }

  if (projectPath && !path.isAbsolute(projectPath)) {
    projectPath = undefined;
    errormsg.push(`A relative paths is not supported for the projectPath parameter`);
  }
  validQueryParams = {
    descriptionurl,
    name,
    kind: validKind,
    type: providedType,
    language: givenLanguage,
    source,
    ttkContext: queryParameters.ttkContext,
    projectPath
  };
  return [validQueryParams, errormsg];
}

async function determineOutputPath(deepLinkParams: Partial<IntegrationParams>): Promise<string | undefined> {
  if (deepLinkParams.projectPath) {
    try {
      const exists = await fs.access(deepLinkParams.projectPath).then(() => true).catch(() => false);
      if (!exists) {
        try {
          await fs.mkdir(deepLinkParams.projectPath);
        } catch (err: unknown) {
          throw new Error(`Error creating directory: ${(err as Error).message}`);
        }
      }
      return deepLinkParams.projectPath;
    } catch (error) {
      return createTemporaryFolder();
    }
  }
  return createTemporaryFolder();
}
