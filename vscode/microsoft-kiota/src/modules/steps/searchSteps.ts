import { KiotaSearchResultItem } from '@microsoft/kiota';
import * as vscode from 'vscode';
import { l10n, QuickPickItem } from 'vscode';

import { BaseStepsState, MultiStepInput } from '.';
import { isValidUrl } from '../../util';
import { isFilePath } from '../../utilities/temporary-folder';
import { shouldResume, validateIsNotEmpty } from './utils';

interface SearchState extends BaseStepsState {
  searchQuery: string;
  searchResults: Record<string, KiotaSearchResultItem>;
  descriptionPath: string;
}

interface OpenState extends BaseStepsState {
  descriptionPath: string;
}

interface SearchItem {
  descriptionUrl?: string;
}

type QuickSearchPickItem = QuickPickItem & SearchItem;


export async function searchSteps(searchCallBack: (searchQuery: string) => Thenable<Record<string, KiotaSearchResultItem> | undefined>) {
  const state: Partial<SearchState & OpenState> = {};
  const title = l10n.t('Add an API description');
  let step = 1;
  let totalSteps = 2;
  async function inputPathOrSearch(input: MultiStepInput, state: Partial<SearchState & OpenState>) {
    const selectedOption = await input.showQuickPick({
      title,
      step: step++,
      totalSteps: totalSteps,
      placeholder: l10n.t('Search or browse a path to an API description'),
      items: [{ label: l10n.t('Search') }, { label: l10n.t('Browse path') }],
      validate: validateIsNotEmpty,
      shouldResume: shouldResume
    });
    if (selectedOption?.label === l10n.t('Search')) {
      return (input: MultiStepInput) => inputSearch(input, state);
    }
    else if (selectedOption?.label === l10n.t('Browse path')) {
      const fileUri = await input.showOpenDialog({
        canSelectMany: false,
        openLabel: 'Select',
        canSelectFolders: false,
        canSelectFiles: true
      });

      if (fileUri && fileUri[0]) {
        state.descriptionPath = fileUri[0].fsPath;
      }
    }
  }

  async function inputSearch(input: MultiStepInput, state: Partial<SearchState & OpenState>) {
    state.searchQuery = await input.showInputBox({
      title,
      step: step++,
      totalSteps: totalSteps,
      value: state.searchQuery ?? '',
      prompt: l10n.t('Search or paste a path to an API description'),
      validate: validateIsNotEmpty,
      shouldResume: shouldResume
    });
    if (state.searchQuery && (isValidUrl(state.searchQuery) || isFilePath(state.searchQuery))) {
      state.descriptionPath = state.searchQuery;
      return;
    }
    state.searchResults = await searchCallBack(state.searchQuery);
    if (state.searchResults && Object.keys(state.searchResults).length > 0) {
      return (input: MultiStepInput) => pickSearchResult(input, state);
    } else {
      vscode.window.showErrorMessage(l10n.t('No results found. Try pasting a path or url instead.'));
      return;
    }
  }

  async function pickSearchResult(input: MultiStepInput, state: Partial<SearchState & OpenState>) {
    const items: QuickSearchPickItem[] = [];
    if (state.searchResults) {
      for (const key of Object.keys(state.searchResults)) {
        const value = state.searchResults[key];
        items.push({ label: key, description: value.Description, descriptionUrl: value.DescriptionUrl });
      }
    }
    const pick = await input.showQuickPick({
      title,
      step: step++,
      totalSteps: totalSteps + 1,
      placeholder: l10n.t('Pick a search result'),
      items: items,
      shouldResume: shouldResume
    });
    state.descriptionPath = items.find(x => x.label === pick?.label)?.descriptionUrl || '';
  }
  await MultiStepInput.run(input => inputPathOrSearch(input, state), () => step -= 2);
  return state;
}