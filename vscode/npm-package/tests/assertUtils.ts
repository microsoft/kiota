import { KiotaLogEntry, LogLevel } from "../types";

export function existsEqualOrGreaterThanLevelLogs(logs: KiotaLogEntry[] | undefined, level: LogLevel): boolean {
  if (!logs) return false;
  return logs.some((log) => log.level >= level);
}