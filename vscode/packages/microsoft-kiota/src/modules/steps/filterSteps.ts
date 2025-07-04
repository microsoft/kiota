import { l10n } from "vscode";
import { BaseStepsState, MultiStepInput } from ".";
import { shouldResume } from "./utils";

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
  await MultiStepInput.run(input => inputFilterQuery(input, state), () => step -= 2);
  return state;
}
