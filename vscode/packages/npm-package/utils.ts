import { KiotaLogEntry, MaturityLevel, DependencyType, LogLevel } from './types.js';

export function getLogEntriesForLevel(logEntries: KiotaLogEntry[], ...levels: LogLevel[]): KiotaLogEntry[] {
  return logEntries.filter((entry) => levels.indexOf(entry.level) !== -1);
}

export function maturityLevelToString(level: MaturityLevel): string {
  switch (level) {
    case MaturityLevel.experimental:
      return "experimental";
    case MaturityLevel.preview:
      return "preview";
    case MaturityLevel.stable:
      return "stable";
    default:
      throw new Error("unknown level");
  }
}

export function dependencyTypeToString(type: DependencyType): string {
  switch (type) {
    case DependencyType.abstractions:
      return "abstractions";
    case DependencyType.serialization:
      return "serialization";
    case DependencyType.authentication:
      return "authentication";
    case DependencyType.http:
      return "http";
    case DependencyType.bundle:
      return "bundle";
    case DependencyType.additional:
      return "additional";
    default:
      throw new Error("unknown type");
  }
}

export function checkForSuccess(results: KiotaLogEntry[]) {
  for (const result of results) {
    if (result && result.message) {
      if (result.message.includes("Generation completed successfully")) {
        return true;
      }
    }
  }
  return false;
}