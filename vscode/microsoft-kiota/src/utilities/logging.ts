import * as vscode from 'vscode';
import { LogOutputChannel } from 'vscode';
import { getLogEntriesForLevel, KiotaLogEntry, LogLevel } from '../kiotaInterop';

export async function exportLogsAndShowErrors(result: KiotaLogEntry[], kiotaOutputChannel: LogOutputChannel): Promise<void> {
  const errorMessages = result
    ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error)
    : [];

  result.forEach((element) => {
    logFromLogLevel(element, kiotaOutputChannel);
  });
  if (errorMessages.length > 0) {
    await Promise.all(errorMessages.map((element) => {
      return vscode.window.showErrorMessage(element.message);
    }));
  }
}

export function logFromLogLevel(entry: KiotaLogEntry, kiotaOutputChannel: LogOutputChannel): void {
  switch (entry.level) {
    case LogLevel.critical:
    case LogLevel.error:
      kiotaOutputChannel.error(entry.message);
      break;
    case LogLevel.warning:
      kiotaOutputChannel.warn(entry.message);
      break;
    case LogLevel.debug:
      kiotaOutputChannel.debug(entry.message);
      break;
    case LogLevel.trace:
      kiotaOutputChannel.trace(entry.message);
      break;
    default:
      kiotaOutputChannel.info(entry.message);
      break;
  }
}

export function showLogs(kiotaOutputChannel: LogOutputChannel): void {
  kiotaOutputChannel.show();
}
