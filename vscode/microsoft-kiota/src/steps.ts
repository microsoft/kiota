import { QuickPickItem, window, Disposable, QuickInputButton, QuickInput, QuickInputButtons, workspace, l10n, Uri } from 'vscode';
import { allGenerationLanguages, generationLanguageToString, KiotaSearchResultItem, LanguagesInformation, maturityLevelToString } from './kiotaInterop';
import { kiotaLockFile } from './extension';


export async function openSteps() {
    const state = {} as Partial<OpenState>;
    const title = l10n.t('Open an API description');
    let step = 1;
    let totalSteps = 1;
    async function inputPathOrUrl(input: MultiStepInput, state: Partial<OpenState>) {
        state.descriptionPath = await input.showInputBox({
            title,
            step: step++,
            totalSteps: totalSteps,
            value: state.descriptionPath || '',
            prompt: l10n.t('A path or url to an OpenAPI description'),
            validate: validateIsNotEmpty,
            shouldResume: shouldResume
        });
    }
    await MultiStepInput.run(input => inputPathOrUrl(input, state), () => step-=2);
    return state;
};

export async function searchLockSteps() {
    const state = {} as Partial<SearchLockState>;
    let step = 1;
    let totalSteps = 1;
    const title = l10n.t('Open a lock file');
    async function pickSearchResult(input: MultiStepInput, state: Partial<SearchLockState>) {
        const searchResults = await workspace.findFiles(`**/${kiotaLockFile}`);
        const items = searchResults.map(x => 
        { 
            return {
                label: x.path.replace(workspace.getWorkspaceFolder(x)?.uri.path || '', ''),
            } as QuickPickItem;
        });
        const pick = await input.showQuickPick({
            title,
            step: step++,
            totalSteps: totalSteps,
            placeholder: l10n.t('Pick a lock file'),
            items: items,
            shouldResume: shouldResume
        });
        state.lockFilePath = searchResults.find(x => x.path.replace(workspace.getWorkspaceFolder(x)?.uri.path || '', '') === pick?.label);
    }
    await MultiStepInput.run(input => pickSearchResult(input, state), () => step-=2);
    return state;
}

export async function filterSteps(existingFilter: string, filterCallback: (searchQuery: string) => void) {
    const state = {} as Partial<BaseStepsState>;
    const title = l10n.t('Filter the API description');
    let step = 1;
    let totalSteps = 1;
    async function inputFilterQuery(input: MultiStepInput, state: Partial<BaseStepsState>) {
        await input.showInputBox({
            title,
            step: step++,
            totalSteps: totalSteps,
            value: existingFilter,
            prompt: l10n.t('Enter a filter'),
            validate: x => {
                filterCallback(x.length === 0 && existingFilter.length > 0 ? existingFilter : x);
                existingFilter = '';
                return Promise.resolve(undefined);
            },
            shouldResume: shouldResume
        });
    }
    await MultiStepInput.run(input => inputFilterQuery(input, state), () => step-=2);
    return state;
}

export async function searchSteps(searchCallBack: (searchQuery: string) => Promise<Record<string, KiotaSearchResultItem> | undefined>) {
    const state = {} as Partial<SearchState>;
    const title = l10n.t('Search for an API description');
    let step = 1;
    let totalSteps = 2;
    async function inputSearchQuery(input: MultiStepInput, state: Partial<SearchState>) {
        state.searchQuery = await input.showInputBox({
            title,
            step: step++,
            totalSteps: totalSteps,
            value: state.searchQuery || '',
            prompt: l10n.t('Enter a search query'),
            validate: validateIsNotEmpty,
            shouldResume: shouldResume
        });

        state.searchResults = await searchCallBack(state.searchQuery);
        return (input: MultiStepInput) => pickSearchResult(input, state);
    }
    async function pickSearchResult(input: MultiStepInput, state: Partial<SearchState>) {
        const items: QuickSearchPickItem[] = [];
        if(state.searchResults) {
            for (const key of Object.keys(state.searchResults)) {
                const value = state.searchResults[key];
                items.push({label: key, description: value.Description, descriptionUrl: value.DescriptionUrl});
            }
        }
        const pick = await input.showQuickPick({
            title,
            step: step++,
            totalSteps: totalSteps,
            placeholder: l10n.t('Pick a search result'),
            items: items,
            shouldResume: shouldResume
        });
        state.descriptionPath = items.find(x => x.label === pick?.label)?.descriptionUrl || '';
    }
    await MultiStepInput.run(input => inputSearchQuery(input, state), () => step-=2);
    return state;
}

interface SearchItem {
    descriptionUrl?: string;
}
type QuickSearchPickItem = QuickPickItem & SearchItem;

export async function generateSteps(existingConfiguration: Partial<GenerateState>, languagesInformation?: LanguagesInformation) {
    const state = {...existingConfiguration} as Partial<GenerateState>;
    if (existingConfiguration.clientClassName && existingConfiguration.clientNamespaceName && existingConfiguration.outputPath && existingConfiguration.language &&
        typeof existingConfiguration.clientNamespaceName === 'string' && typeof existingConfiguration.outputPath === 'string' && typeof existingConfiguration.language === 'string' &&
        existingConfiguration.clientClassName.length > 0 && existingConfiguration.clientNamespaceName.length > 0 && existingConfiguration.outputPath.length > 0 && existingConfiguration.language.length > 0) {
        return state;
    }

    if(typeof state.outputPath === 'string') {
        state.outputPath = workspace.asRelativePath(state.outputPath);
    }
    const title = l10n.t('Generate an API client');
    let step = 1;
    let totalSteps = 4;
    async function inputClientClassName(input: MultiStepInput, state: Partial<GenerateState>) {
		state.clientClassName = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: state.clientClassName || '',
            placeholder: 'ApiClient',
			prompt: l10n.t('Choose a name for the client class'),
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => inputClientNamespaceName(input, state);
	}
    async function inputClientNamespaceName(input: MultiStepInput, state: Partial<GenerateState>) {
		state.clientNamespaceName = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: typeof state.clientNamespaceName === 'string' ? state.clientNamespaceName : '',
			placeholder: 'ApiSDK',
			prompt: l10n.t('Choose a name for the client class namespace'),
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => inputOutputPath(input, state);
	}
    async function inputOutputPath(input: MultiStepInput, state: Partial<GenerateState>) {
		state.outputPath = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: typeof state.outputPath === 'string' ? state.outputPath : '',
			placeholder: 'myproject/apiclient',
			prompt: l10n.t('Enter an output path relative to the root of the project'),
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => pickLanguage(input, state);
	}
    async function pickLanguage(input: MultiStepInput, state: Partial<GenerateState>) {
        const items = allGenerationLanguages.map(x => {
            const lngName = generationLanguageToString(x);
            const lngInfo = languagesInformation ? languagesInformation[lngName] : undefined;
            const lngMaturity = lngInfo ? ` - ${maturityLevelToString(lngInfo.MaturityLevel)}` : '';
            return {
                label: `${lngName}${lngMaturity}`,
                languageName: lngName,
            } as (QuickPickItem & {languageName: string});
        });
		const pick = await input.showQuickPick({
			title,
			step: step++,
			totalSteps: totalSteps,
			placeholder: l10n.t('Pick a language'),
			items,
			activeItem: typeof state.language === 'string' ? items.find(x => x.languageName === state.language) : undefined,
			shouldResume: shouldResume
		});
		state.language = pick.label.split('-')[0].trim();
	}
    await MultiStepInput.run(input => inputClientClassName(input, state), () => step-=2);
    return state;
}

function shouldResume() {
    // Could show a notification with the option to resume.
    return new Promise<boolean>((resolve, reject) => {
        // noop
    });
}

function validateIsNotEmpty(value: string) {
    return Promise.resolve(value.length > 0 ? undefined : 'Required');
}

interface BaseStepsState {
    title: string;
    step: number;
    totalSteps: number;
}

interface SearchState extends BaseStepsState {
    searchQuery: string;
    searchResults: Record<string, KiotaSearchResultItem>;
    descriptionPath: string;
}

interface OpenState extends BaseStepsState {
    descriptionPath: string;
}

interface SearchLockState extends BaseStepsState {
    lockFilePath: Uri;
}

interface GenerateState extends BaseStepsState {
    clientClassName: string;
    clientNamespaceName: QuickPickItem | string;
    language: QuickPickItem | string;
    outputPath: QuickPickItem | string;
}
class InputFlowAction {
	static back = new InputFlowAction();
	static cancel = new InputFlowAction();
	static resume = new InputFlowAction();
}
type InputStep = (input: MultiStepInput) => Thenable<InputStep | void>;

interface QuickPickParameters<T extends QuickPickItem> {
	title: string;
	step: number;
	totalSteps: number;
	items: T[];
	activeItem?: T;
	ignoreFocusOut?: boolean;
	placeholder: string;
	buttons?: QuickInputButton[];
	shouldResume: () => Thenable<boolean>;
}

interface InputBoxParameters {
	title: string;
	step: number;
	totalSteps: number;
	value: string;
	prompt: string;
	validate: (value: string) => Promise<string | undefined>;
	buttons?: QuickInputButton[];
	ignoreFocusOut?: boolean;
	placeholder?: string;
	shouldResume: () => Thenable<boolean>;
}

class MultiStepInput {

	static async run<T>(start: InputStep, onNavBack?: () => void) {
		const input = new MultiStepInput();
		return input.stepThrough(start, onNavBack);
	}

	private current?: QuickInput;
	private steps: InputStep[] = [];

	private async stepThrough<T>(start: InputStep, onNavBack?: () => void) {
		let step: InputStep | void = start;
		while (step) {
			this.steps.push(step);
			if (this.current) {
				this.current.enabled = false;
				this.current.busy = true;
			}
			try {
				step = await step(this);
			} catch (err) {
				if (err === InputFlowAction.back) {
                    if (onNavBack) {
                        onNavBack();
                    }
					this.steps.pop();
					step = this.steps.pop();
				} else if (err === InputFlowAction.resume) {
					step = this.steps.pop();
				} else if (err === InputFlowAction.cancel) {
					step = undefined;
				} else {
					throw err;
				}
			}
		}
		if (this.current) {
			this.current.dispose();
		}
	}

	async showQuickPick<T extends QuickPickItem, P extends QuickPickParameters<T>>({ title, step, totalSteps, items, activeItem, ignoreFocusOut, placeholder, buttons, shouldResume }: P) {
		const disposables: Disposable[] = [];
		try {
			return await new Promise<T | (P extends { buttons: (infer I)[] } ? I : never)>((resolve, reject) => {
				const input = window.createQuickPick<T>();
				input.title = title;
				input.step = step;
				input.totalSteps = totalSteps;
				input.ignoreFocusOut = ignoreFocusOut ?? false;
				input.placeholder = placeholder;
				input.items = items;
				input.buttons = [
					...(this.steps.length > 1 ? [QuickInputButtons.Back] : []),
					...(buttons || [])
				];
				disposables.push(
					input.onDidTriggerButton(item => {
						if (item === QuickInputButtons.Back) {
							reject(InputFlowAction.back);
						} else {
							resolve(<any>item);
						}
					}),
					input.onDidChangeSelection(items => resolve(items[0])),
					input.onDidHide(() => {
						(async () => {
							reject(shouldResume && await shouldResume() ? InputFlowAction.resume : InputFlowAction.cancel);
						})()
							.catch(reject);
					})
				);
				if (this.current) {
					this.current.dispose();
				}
				this.current = input;
				this.current.show();
                if (activeItem) {
					input.activeItems = [activeItem];
				}
			});
		} finally {
			disposables.forEach(d => d.dispose());
		}
	}

	async showInputBox<P extends InputBoxParameters>({ title, step, totalSteps, value, prompt, validate, buttons, ignoreFocusOut, placeholder, shouldResume }: P) {
		const disposables: Disposable[] = [];
		try {
			return await new Promise<string | (P extends { buttons: (infer I)[] } ? I : never)>((resolve, reject) => {
				const input = window.createInputBox();
				input.title = title;
				input.step = step;
				input.totalSteps = totalSteps;
				input.value = value || '';
				input.prompt = prompt;
				input.ignoreFocusOut = ignoreFocusOut ?? false;
				input.placeholder = placeholder;
				input.buttons = [
					...(this.steps.length > 1 ? [QuickInputButtons.Back] : []),
					...(buttons || [])
				];
				let validating = validate('');
				disposables.push(
					input.onDidTriggerButton(item => {
						if (item === QuickInputButtons.Back) {
							reject(InputFlowAction.back);
						} else {
							resolve(<any>item);
						}
					}),
					input.onDidAccept(async () => {
						const value = input.value;
						input.enabled = false;
						input.busy = true;
						if (!(await validate(value))) {
							resolve(value);
						}
						input.enabled = true;
						input.busy = false;
					}),
					input.onDidChangeValue(async text => {
						const current = validate(text);
						validating = current;
						const validationMessage = await current;
						if (current === validating) {
							input.validationMessage = validationMessage;
						}
					}),
					input.onDidHide(() => {
						(async () => {
							reject(shouldResume && await shouldResume() ? InputFlowAction.resume : InputFlowAction.cancel);
						})()
							.catch(reject);
					})
				);
				if (this.current) {
					this.current.dispose();
				}
				this.current = input;
				this.current.show();
			});
		} finally {
			disposables.forEach(d => d.dispose());
		}
	}
}