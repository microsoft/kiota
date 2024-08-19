import * as vscode from "vscode";
import { getLogEntriesForLevel, KiotaLogEntry, LogLevel } from "../kiotaInterop";

let kiotaOutputChannel: vscode.LogOutputChannel;
kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", {
  log: true,
});

async function exportLogsAndShowErrors(result: KiotaLogEntry[]): Promise<void> {
  const errorMessages = result
    ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error)
    : [];

  result.forEach((element) => {
    logFromLogLevel(element);
  });
  if (errorMessages.length > 0) {
    await Promise.all(errorMessages.map((element) => {
      return vscode.window.showErrorMessage(element.message);
    }));
  }
}

function logFromLogLevel(entry: KiotaLogEntry): void {
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

export {
  kiotaOutputChannel,
  exportLogsAndShowErrors,
};