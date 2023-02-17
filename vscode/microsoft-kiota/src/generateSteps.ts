import { QuickPickItem, window, Disposable, QuickInputButton, QuickInput, ExtensionContext, QuickInputButtons } from 'vscode';
import { allGenerationLanguages, generationLanguageToString } from './kiotaInterop';

export async function generateSteps() {
    const state = {} as Partial<State>;
    const title = 'Generate an API client';
    let step = 1;
    let totalSteps = 4;
    async function inputClientClassName(input: MultiStepInput, state: Partial<State>) {
		state.clientClassName = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: state.clientClassName || '',
            placeholder: 'ApiClient',
			prompt: 'Choose a name for the client class',
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => inputClientNamespaceName(input, state);
	}
    async function inputClientNamespaceName(input: MultiStepInput, state: Partial<State>) {
		state.clientNamespaceName = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: typeof state.clientNamespaceName === 'string' ? state.clientNamespaceName : '',
			placeholder: 'ApiSDK',
			prompt: 'Choose a name for the client class namespace',
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => inputOutputPath(input, state);
	}
    async function inputOutputPath(input: MultiStepInput, state: Partial<State>) {
		state.outputPath = await input.showInputBox({
			title,
			step: step++,
			totalSteps: totalSteps,
			value: typeof state.outputPath === 'string' ? state.outputPath : '',
			placeholder: 'myproject/apiclient',
			prompt: 'Enter an output path relative to the root of the project',
			validate: validateIsNotEmpty,
			shouldResume: shouldResume
		});
		return (input: MultiStepInput) => pickLanguage(input, state);
	}
    async function pickLanguage(input: MultiStepInput, state: Partial<State>) {
		const pick = await input.showQuickPick({
			title,
			step: 1,
			totalSteps: 3,
			placeholder: 'Pick a language',
			items: allGenerationLanguages.map(x => { return {label: generationLanguageToString(x)};}),
			activeItem: typeof state.language !== 'string' ? state.language : undefined,
			shouldResume: shouldResume
		});
		state.language = pick;
	}
    await MultiStepInput.run(input => inputClientClassName(input, state));
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

interface State {
    title: string;
    step: number;
    totalSteps: number;
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

	static async run<T>(start: InputStep) {
		const input = new MultiStepInput();
		return input.stepThrough(start);
	}

	private current?: QuickInput;
	private steps: InputStep[] = [];

	private async stepThrough<T>(start: InputStep) {
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
				if (activeItem) {
					input.activeItems = [activeItem];
				}
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