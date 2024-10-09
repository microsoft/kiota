import AdmZip from 'adm-zip';
import { createHash } from 'crypto';
import * as fs from 'fs';
import * as https from 'https';
import * as path from 'path';
import * as vscode from "vscode";


const kiotaInstallStatusKey = "kiotaInstallStatus";
const installDelayInMs = 30000; // 30 seconds
async function runIfNotLocked(context: vscode.ExtensionContext, action: () => Promise<void>) {
  const installStartTimeStamp = context.globalState.get<number>(kiotaInstallStatusKey);
  const currentTimeStamp = new Date().getTime();
  if (!installStartTimeStamp || (currentTimeStamp - installStartTimeStamp) > installDelayInMs) {
    //locking the context to prevent multiple downloads across multiple instances of vscode
    //overriding after 30 seconds to prevent stale locks
    await context.globalState.update(kiotaInstallStatusKey, currentTimeStamp);
    try {
      await action();
    } finally {
      await context.globalState.update(kiotaInstallStatusKey, undefined);
    }
  }
}
export async function ensureKiotaIsPresent(context: vscode.ExtensionContext) {
    const runtimeDependencies = getRuntimeDependenciesPackages(context);
    const currentPlatform = getCurrentPlatform();
    const installPath = getKiotaPathInternal(context, false);
    if (installPath) {
        if (!fs.existsSync(installPath) || fs.readdirSync(installPath).length === 0) {
            await vscode.window.withProgress({
                location: vscode.ProgressLocation.Notification,
                cancellable: false,
                title: vscode.l10n.t("Downloading kiota...")

            }, async (progress, _) => {
                await (async () => {
                    const isOnline = (await import('is-online')).default;
                    const online = await isOnline();
                    if (!online) {
                        await vscode.window.showErrorMessage(
                            vscode.l10n.t("Downloading kiota requires an internet connection. Please check your connection and try again.")
                        );
                        return;
                    }
                })();
              await runIfNotLocked(context, async () => {
                try {
                        const packageToInstall = runtimeDependencies.find((p) => p.platformId === currentPlatform);
                        if (!packageToInstall) {
                            throw new Error("Could not find package to install");
                        }
                        fs.mkdirSync(installPath, { recursive: true });
                        const zipFilePath = `${installPath}.zip`;
                        await downloadFileFromUrl(getDownloadUrl(context, currentPlatform), zipFilePath);
                        if (await doesFileHashMatch(zipFilePath, packageToInstall.sha256)) {
                            unzipFile(zipFilePath, installPath);
                            const kiotaPath = getKiotaPathInternal(context);
                            if ((currentPlatform.startsWith(linuxPlatform) || currentPlatform.startsWith(osxPlatform)) && kiotaPath) {
                                makeExecutable(kiotaPath);
                            }
                        } else {
                            throw new Error("Hash mismatch");
                        }
                    } catch(error) {
                        await vscode.window.showErrorMessage(
                            vscode.l10n.t("Kiota download failed. Try closing all Visual Studio Code windows and open only one. Check the extension host logs for more information.")
                        );
                        fs.rmdirSync(installPath, { recursive: true });
                    }
                });
            });
        }
    }
}

let kiotaPath: string | undefined;

export function getKiotaPath(context: vscode.ExtensionContext): string {
    if (!kiotaPath) {
        kiotaPath = getKiotaPathInternal(context);
        if(!kiotaPath) {
            throw new Error("Could not find kiota");
        }
    }
    return kiotaPath;
}
function makeExecutable(path: string) {
    fs.chmodSync(path, 0o755);
}

const binariesRootDirectory = '.kiotabin';
function getKiotaPathInternal(context: vscode.ExtensionContext, withFileName = true): string | undefined {
    const fileName = process.platform === 'win32' ? 'kiota.exe' : 'kiota';
    const runtimeDependencies = getRuntimeDependenciesPackages(context);
    const currentPlatform = getCurrentPlatform();
    const packageToInstall = runtimeDependencies.find((p) => p.platformId === currentPlatform);
    if (packageToInstall) {
        const installPath = context.asAbsolutePath(binariesRootDirectory);
        const directoryPath = path.join(installPath, context.extension.packageJSON.kiotaVersion, currentPlatform);
        if (withFileName) {
            return path.join(directoryPath, fileName);
        }
        return directoryPath;
    }
    return undefined;
}

function unzipFile(zipFilePath: string, destinationPath: string) {
    const zip = new AdmZip(zipFilePath);
    zip.extractAllTo(destinationPath, true);
}

async function doesFileHashMatch(destinationPath: string, hashValue: string) : Promise<boolean> {
    const hash = createHash('sha256');
    return new Promise((resolve, reject) => {
        fs.createReadStream(destinationPath).pipe(hash).on('finish', () => {;
            const computedValue = hash.digest('hex');
            hash.destroy();
            resolve(computedValue.toUpperCase() === hashValue.toUpperCase());
        });
    });
}

function downloadFileFromUrl(url: string, destinationPath: string) {
    return new Promise((resolve, reject) => {
        https.get(url, (response) => {
            if (response.statusCode && response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
                resolve(downloadFileFromUrl(response.headers.location, destinationPath));
            } else {
                const filePath = fs.createWriteStream(destinationPath);
                response.pipe(filePath);
                filePath.on('finish', () => {
                    filePath.close();
                    resolve(undefined);
                });
            }
        });
    });
}
const baseDownloadUrl = "https://github.com/microsoft/kiota/releases/download";
function getDownloadUrl(context: vscode.ExtensionContext, platform: string): string {
    return `${baseDownloadUrl}/v${context.extension.packageJSON.kiotaVersion}/${platform}.zip`;
}

function getRuntimeDependenciesPackages(context: vscode.ExtensionContext): Package[] {
    const packageJSON = context.extension.packageJSON;
    if (packageJSON.runtimeDependencies) {
        return JSON.parse(JSON.stringify(<Package[]>packageJSON.runtimeDependencies));
    }
    throw new Error("No runtime dependencies found");
}

export interface Package {
    platformId: string;
    sha256: string;
}

const windowsPlatform = 'win';
const osxPlatform = 'osx';
const linuxPlatform = 'linux';
export function getCurrentPlatform(): string {
    const binPathSegmentOS = process.platform === 'win32' ? windowsPlatform : process.platform === 'darwin' ? osxPlatform : linuxPlatform;
    return `${binPathSegmentOS}-${process.arch}`;
}
