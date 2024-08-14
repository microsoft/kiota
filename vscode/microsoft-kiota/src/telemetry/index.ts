import TelemetryReporter from '@vscode/extension-telemetry';
import * as vscode from "vscode";

export class Telemetry {

  public static reporter: TelemetryReporter;

  public static async initialize() {
    const telemetryInstrumentationKey = await Telemetry.getInstrumentationKey();
    Telemetry.reporter = new TelemetryReporter(telemetryInstrumentationKey);
  }

  public static sendEvent(eventName: string, properties?: { [key: string]: string }) {
    try {
      Telemetry.reporter.sendTelemetryEvent(eventName, properties);
    } catch {
    }
  }

  private static async getInstrumentationKey(): Promise<string> {
    // Step 1: Locate the package.json file
    const packageJsonFiles = await vscode.workspace.findFiles('**/package.json', '**/node_modules/**');

    if (packageJsonFiles.length > 0) {
      const packageJsonUri = packageJsonFiles[0]; // Assuming the first found is the right one

      // Step 2: Read the file contents
      const fileContents = await vscode.workspace.fs.readFile(packageJsonUri);

      // Step 3: Parse the JSON and access the telemetryInstrumentationKey
      const packageJson = JSON.parse(fileContents.toString());
      const instrumentationKey = packageJson["telemetryInstrumentationKey"];

      if (instrumentationKey) {
        return instrumentationKey;
      } else {
        throw new Error('Instrumentation key not found!');
      }
    } else {
      throw new Error('package.json file not found!');
    }
  }
}

export const telemetry = Telemetry.initialize();