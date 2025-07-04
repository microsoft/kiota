import { l10n } from 'vscode';

export function validateIsNotEmpty(value: string) {
  return Promise.resolve(value.length > 0 ? undefined : l10n.t('Required'));
}

export function shouldResume() {
  // Could show a notification with the option to resume.
  return new Promise<boolean>((resolve, reject) => {
    // noop
  });
}