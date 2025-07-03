import { allGenerationLanguages, generationLanguageToString, LanguagesInformation, maturityLevelToString } from '@microsoft/kiota';
import * as path from 'path';
import * as vscode from 'vscode';
import { l10n, QuickPickItem, workspace } from 'vscode';

import { BaseStepsState, MultiStepInput } from '.';
import { findAppPackageDirectory, getWorkspaceJsonDirectory } from '../../util';
import { IntegrationParams, isDeeplinkEnabled } from '../../utilities/deep-linking';
import { isTemporaryDirectory } from '../../utilities/temporary-folder';
import { shouldResume, validateIsNotEmpty } from './utils';

export interface GenerateState extends BaseStepsState {
  generationType: QuickPickItem | string;
  pluginTypes: QuickPickItem | string[];
  pluginName: string;
  clientClassName: string;
  clientNamespaceName: QuickPickItem | string;
  language: QuickPickItem | string;
  outputPath: QuickPickItem | string;
  workingDirectory: string;
}
export async function generateSteps(existingConfiguration: Partial<GenerateState>, languagesInformation?: LanguagesInformation, deepLinkParams?: Partial<IntegrationParams>) {
  const state = { ...existingConfiguration } as Partial<GenerateState>;
  if (existingConfiguration.generationType && existingConfiguration.clientClassName && existingConfiguration.clientNamespaceName && existingConfiguration.outputPath && existingConfiguration.language &&
    typeof existingConfiguration.generationType === 'string' && existingConfiguration.clientNamespaceName === 'string' && typeof existingConfiguration.outputPath === 'string' && typeof existingConfiguration.language === 'string' &&
    existingConfiguration.generationType.length > 0 && existingConfiguration.clientClassName.length > 0 && existingConfiguration.clientNamespaceName.length > 0 && existingConfiguration.outputPath.length > 0 && existingConfiguration.language.length > 0) {
    return state;
  }

  const deeplinkEnabled = deepLinkParams && isDeeplinkEnabled(deepLinkParams);
  const isDeepLinkPluginNameProvided = deeplinkEnabled && state.pluginName;
  const isDeepLinkGenerationTypeProvided = deeplinkEnabled && state.generationType;
  const isDeepLinkPluginTypeProvided = deeplinkEnabled && state.pluginTypes;
  const isDeepLinkLanguageProvided = deeplinkEnabled && state.language;
  const isDeepLinkOutputPathProvided = deeplinkEnabled && state.outputPath;
  const isDeepLinkClientClassNameProvided = deeplinkEnabled && state.clientClassName;

  if (typeof state.outputPath === 'string' && !isTemporaryDirectory(state.outputPath)) {
    state.outputPath = workspace.asRelativePath(state.outputPath);
  }
  const workspaceOpen = vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0;

  let workspaceFolder = getWorkspaceJsonDirectory();
  const appPackagePath = findAppPackageDirectory(workspaceFolder);
  if (appPackagePath) {
    workspaceFolder = appPackagePath;
  }

  if (isDeepLinkOutputPathProvided && deepLinkParams.source && deepLinkParams.source.toLowerCase() === 'ttk') {
    state.workingDirectory = state.outputPath as string;
  }

  let step = 1;
  const folderSelectionOption = l10n.t('Browse your output directory');

  function getOutputPath(workspaceFolder: string, clientName: string) {
    const outputPath = path.join(workspaceFolder, clientName);
    return [
      { label: l10n.t('Default folder'), description: outputPath },
      { label: folderSelectionOption }
    ];
  }

  function updateWorkspaceFolder(name: string | undefined) {
    if (name && (!workspaceOpen)) {
      workspaceFolder = getWorkspaceJsonDirectory(name);
    }
  }
  function getNextStepForGenerationType(generationType: string | QuickPickItem) {
    switch (generationType) {
      case 'client':
        return inputClientClassName;
      case 'plugin':
        return inputPluginName;
      case 'other':
        return chooseOtherGenerationType;
      default:
        throw new Error('unknown generation type');
    }
  }
  async function inputGenerationType(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkGenerationTypeProvided) {
      const items = [
        l10n.t('Client'),
        l10n.t('Copilot plugin'),
        l10n.t('Other')
      ];
      const option = await input.showQuickPick({
        title: l10n.t('What do you want to generate?'),
        step: step++,
        totalSteps: 3,
        placeholder: l10n.t('Select an option'),
        items: items.map(x => ({ label: x })),
        validate: validateIsNotEmpty,
        shouldResume: shouldResume
      });
      if (option.label === l10n.t('Client')) {
        state.generationType = "client";
      }
      else if (option.label === l10n.t('Copilot plugin')) {
        state.generationType = "plugin";
      }
      else if (option.label === l10n.t('Other')) {
        state.generationType = "other";
      }
    }
    let nextStep = getNextStepForGenerationType(state.generationType?.toString() || '');
    return (input: MultiStepInput) => nextStep(input, state);
  }
  async function inputClientClassName(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkClientClassNameProvided) {
      state.clientClassName = await input.showInputBox({
        title: `${l10n.t('Create a new API client')} - ${l10n.t('class')}`,
        step: step++,
        totalSteps: 5,
        value: state.clientClassName ?? '',
        placeholder: 'ApiClient',
        prompt: l10n.t('Choose a name for the client class'),
        validate: validateIsNotEmpty,
        shouldResume: shouldResume
      });
    }
    updateWorkspaceFolder(state.clientClassName);
    return (input: MultiStepInput) => inputClientNamespaceName(input, state);
  }
  async function inputClientNamespaceName(input: MultiStepInput, state: Partial<GenerateState>) {
    state.clientNamespaceName = await input.showInputBox({
      title: `${l10n.t('Create a new API client')} - ${l10n.t('namespace')}`,
      step: step++,
      totalSteps: 5,
      value: typeof state.clientNamespaceName === 'string' ? state.clientNamespaceName : '',
      placeholder: 'ApiSDK',
      prompt: l10n.t('Choose a name for the client class namespace'),
      validate: validateIsNotEmpty,
      shouldResume: shouldResume
    });
    return (input: MultiStepInput) => inputOutputPath(input, state);
  }
  async function inputOutputPath(input: MultiStepInput, state: Partial<GenerateState>) {
    while (true) {
      const selectedOption = await input.showQuickPick({
        title: `${l10n.t('Create a new API client')} - ${l10n.t('output directory')}`,
        step: step++,
        totalSteps: 5,
        placeholder: l10n.t('Enter an output path relative to the root of the project'),
        items: getOutputPath(workspaceFolder, state.clientClassName!),
        shouldResume: shouldResume
      });
      if (selectedOption) {
        if (selectedOption?.label === folderSelectionOption) {
          const folderUri = await input.showOpenDialog({
            canSelectMany: false,
            openLabel: 'Select',
            canSelectFolders: true,
            canSelectFiles: false
          });

          if (folderUri && folderUri[0]) {
            state.outputPath = folderUri[0].fsPath;
          } else {
            continue;
          }
        } else {
          state.outputPath = selectedOption.description;
          if (workspaceOpen) {
            state.workingDirectory = vscode.workspace.workspaceFolders![0].uri.fsPath;
          } else {
            state.workingDirectory = path.dirname(selectedOption.description!);
          }
        }
      }
      state.outputPath = state.outputPath === '' ? 'output' : state.outputPath;
      return (input: MultiStepInput) => pickLanguage(input, state);
    }

  }
  async function pickLanguage(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkLanguageProvided) {
      const items = allGenerationLanguages.map(x => {
        const lngName = generationLanguageToString(x);
        const lngInfo = languagesInformation ? languagesInformation[lngName] : undefined;
        const lngMaturity = lngInfo ? ` - ${maturityLevelToString(lngInfo.MaturityLevel)}` : '';
        return {
          label: `${lngName}${lngMaturity}`,
          languageName: lngName,
        } as (QuickPickItem & { languageName: string });
      });
      const pick = await input.showQuickPick({
        title: `${l10n.t('Create a new API client')} - ${l10n.t('language')}`,
        step: step++,
        totalSteps: 5,
        placeholder: l10n.t('Pick a language'),
        items,
        activeItem: typeof state.language === 'string' ? items.find(x => x.languageName === state.language) : undefined,
        shouldResume: shouldResume
      });
      state.language = pick.label.split('-')[0].trim();
    }
  }
  async function inputPluginName(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkPluginNameProvided) {
      state.pluginName = await input.showInputBox({
        title: `${l10n.t('Create a new plugin')} - ${l10n.t('plugin name')}`,
        step: step++,
        totalSteps: 3,
        value: state.pluginName ?? '',
        placeholder: 'MyPlugin',
        prompt: l10n.t('Choose a name for the plugin'),
        validate: validateIsNotEmpty,
        shouldResume: shouldResume
      });
    }
    state.pluginTypes = ['ApiPlugin'];
    updateWorkspaceFolder(state.pluginName);
    return (input: MultiStepInput) => inputPluginOutputPath(input, state);
  }
  async function chooseOtherGenerationType(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkPluginTypeProvided) {
      const items = ['API Manifest', 'Open AI Plugin'].map(x => ({ label: x }) as QuickPickItem);
      const pluginTypes = await input.showQuickPick({
        title: l10n.t('Choose a type'),
        step: step++,
        totalSteps: 4,
        placeholder: l10n.t('Select an option'),
        items: items,
        validate: validateIsNotEmpty,
        shouldResume: shouldResume
      });
      pluginTypes.label === 'API Manifest' ? state.pluginTypes = ['ApiManifest'] : state.pluginTypes = ['OpenAI'];

    }

    Array.isArray(state.pluginTypes) && state.pluginTypes.includes('ApiManifest') ?
      state.generationType = 'apimanifest' : state.generationType = 'plugin';
    return (input: MultiStepInput) => inputOtherGenerationTypeName(input, state);
  }
  async function inputPluginOutputPath(input: MultiStepInput, state: Partial<GenerateState>) {
    while (!isDeepLinkOutputPathProvided) {
      const selectedOption = await input.showQuickPick({
        title: `${l10n.t('Create a new plugin')} - ${l10n.t('output directory')}`,
        step: step++,
        totalSteps: 4,
        placeholder: l10n.t('Enter an output path relative to the root of the project'),
        items: getOutputPath(workspaceFolder, state.clientClassName!),
        shouldResume: shouldResume
      });
      if (selectedOption) {
        if (selectedOption?.label === folderSelectionOption) {
          const folderUri = await input.showOpenDialog({
            canSelectMany: false,
            openLabel: 'Select',
            canSelectFolders: true,
            canSelectFiles: false
          });

          if (folderUri && folderUri[0]) {
            state.outputPath = folderUri[0].fsPath;
          } else {
            continue;
          }
        } else {
          state.outputPath = selectedOption.description;
          if (workspaceOpen) {
            state.workingDirectory = vscode.workspace.workspaceFolders![0].uri.fsPath;
          } else {
            state.workingDirectory = path.dirname(selectedOption.description!);
          }
        }
      }
      state.outputPath = state.outputPath === '' ? 'output' : state.outputPath;
      break;
    }
  }
  async function inputOtherGenerationTypeName(input: MultiStepInput, state: Partial<GenerateState>) {
    if (!isDeepLinkPluginNameProvided) {
      const isManifest = state.pluginTypes && Array.isArray(state.pluginTypes) && state.pluginTypes.includes('ApiManifest');
      state.pluginName = await input.showInputBox({
        title: `${isManifest ? l10n.t('Create a new manifest') : l10n.t('Create a new OpenAI plugin')} - ${l10n.t('output name')}`,
        step: step++,
        totalSteps: 4,
        value: state.pluginName ?? '',
        placeholder: `${isManifest ? 'MyManifest' : 'MyOpenAIPlugin'}`,
        prompt: `${isManifest ? l10n.t('Choose a name for the manifest') : l10n.t('Choose a name for the OpenAI plugin')}`,
        validate: validateIsNotEmpty,
        shouldResume: shouldResume
      });
    }
    updateWorkspaceFolder(state.pluginName);
    return (input: MultiStepInput) => inputOtherGenerationTypeOutputPath(input, state);
  }
  async function inputOtherGenerationTypeOutputPath(input: MultiStepInput, state: Partial<GenerateState>) {
    while (true) {
      const isManifest = state.pluginTypes && Array.isArray(state.pluginTypes) && state.pluginTypes.includes('ApiManifest');

      const selectedOption = await input.showQuickPick({
        title: `${isManifest ? l10n.t('Create a new manifest') : l10n.t('Create a new OpenAI plugin')} - ${l10n.t('output directory')}`,
        step: step++,
        totalSteps: 4,
        placeholder: l10n.t('Enter an output path relative to the root of the project'),
        items: getOutputPath(workspaceFolder, state.clientClassName!),
        shouldResume: shouldResume
      });
      if (selectedOption) {
        if (selectedOption?.label === folderSelectionOption) {
          const folderUri = await input.showOpenDialog({
            canSelectMany: false,
            openLabel: 'Select',
            canSelectFolders: true,
            canSelectFiles: false
          });

          if (folderUri && folderUri[0]) {
            state.outputPath = folderUri[0].fsPath;
          } else {
            continue;
          }
        } else {
          state.outputPath = selectedOption.description;
          if (workspaceOpen) {
            state.workingDirectory = vscode.workspace.workspaceFolders![0].uri.fsPath;
          } else {
            state.workingDirectory = path.dirname(selectedOption.description!);
          }
        }
      }
      if (state.outputPath === '') {
        state.outputPath = 'output';
      }
      break;
    }

  }
  await MultiStepInput.run(input => inputGenerationType(input, state), () => step -= 2);
  if (!state.workingDirectory) {
    state.workingDirectory = workspaceOpen ? vscode.workspace.workspaceFolders![0].uri.fsPath : state.outputPath as string;
  }
  return state;
}