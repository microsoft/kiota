import AdmZip from 'adm-zip';
import * as https from 'https';
import * as fs from 'fs';
import * as path from 'path';
import { getKiotaConfig } from './config';

import runtimeJson from './runtime.json';

const kiotaInstallStatusKey = "kiotaInstallStatus";
const installDelayInMs = 30000; // 30 seconds
const state: { [key: string]: any } = {};

let kiotaPath: string | undefined;
const binariesRootDirectory = '.kiotabin';
const baseDownloadUrl = "https://github.com/microsoft/kiota/releases/download";

export interface Package {
  platformId: string;
  sha256: string;
}

const windowsPlatform = 'win';
const osxPlatform = 'osx';
const linuxPlatform = 'linux';

async function runIfNotLocked(action: () => Promise<void>) {
  const installStartTimeStamp = state[kiotaInstallStatusKey];
  const currentTimeStamp = new Date().getTime();
  if (!installStartTimeStamp || (currentTimeStamp - installStartTimeStamp) > installDelayInMs) {
    //locking the context to prevent multiple downloads across multiple instances
    //overriding after 30 seconds to prevent stale locks
    state[kiotaInstallStatusKey] = currentTimeStamp;
    try {
      await action();
    } finally {
      state[kiotaInstallStatusKey] = undefined;
    }
  }
}

export async function ensureKiotaIsPresent() {
  const installPath = getKiotaPathInternal(false);
  if (installPath) {
    await ensureKiotaIsPresentInPath(installPath);
  }
}

export async function ensureKiotaIsPresentInPath(installPath: string) {
  const runtimeDependencies = getRuntimeDependenciesPackages();
  const currentPlatform = getCurrentPlatform();
  if (installPath) {
    if (!fs.existsSync(installPath) || fs.readdirSync(installPath).length === 0) {
      await runIfNotLocked(async () => {
        try {
          const packageToInstall = runtimeDependencies.find((p) => p.platformId === currentPlatform);
          if (!packageToInstall) {
            throw new Error("Could not find package to install");
          }
          fs.mkdirSync(installPath, { recursive: true });
          const zipFilePath = `${installPath}.zip`;
          // If env variable that points to kiota binary zip exists, use it to copy the file instead of downloading it
          const kiotaBinaryZip = process.env.KIOTA_SIDELOADING_BINARY_ZIP_PATH;
          if (kiotaBinaryZip && fs.existsSync(kiotaBinaryZip)) {
            fs.copyFileSync(kiotaBinaryZip, zipFilePath);
          } else {
            const downloadUrl = getDownloadUrl(currentPlatform);
            await downloadFileFromUrl(downloadUrl, zipFilePath);
          }
          unzipFile(zipFilePath, installPath);
          const kiotaPath = getKiotaPathInternal();
          if ((currentPlatform.startsWith(linuxPlatform) || currentPlatform.startsWith(osxPlatform)) && kiotaPath) {
            makeExecutable(kiotaPath);
          }
        } catch (error) {
          fs.rmdirSync(installPath, { recursive: true });
          throw error; //TODO revert
        }
      });
    }
  }
}

export function getKiotaPath(): string {
  if (!kiotaPath) {
    kiotaPath = getKiotaPathInternal();
    if (!kiotaPath) {
      throw new Error("Could not find kiota");
    }
  }
  return kiotaPath;
}

function makeExecutable(path: string) {
  fs.chmodSync(path, 0o755);
}

function getBaseDir(): string {
  return getKiotaConfig().binaryLocation || path.resolve(__dirname);
}

function getRuntimeVersion(): string {
  return getKiotaConfig().binaryVersion || runtimeJson.kiotaVersion;
}

function getKiotaPathInternal(withFileName = true): string | undefined {
  const fileName = process.platform === 'win32' ? 'kiota.exe' : 'kiota';
  const runtimeDependencies = getRuntimeDependenciesPackages();
  const currentPlatform = getCurrentPlatform();
  const packageToInstall = runtimeDependencies.find((p) => p.platformId === currentPlatform);
  const baseDir = getBaseDir();
  const runtimeVersion = getRuntimeVersion();
  if (packageToInstall) {
    const installPath = path.join(baseDir, binariesRootDirectory);
    const directoryPath = path.join(installPath, runtimeVersion, currentPlatform);
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

function downloadFileFromUrl(url: string, destinationPath: string): Promise<void> {
  return new Promise((resolve) => {
    https.get(url, (response: any) => {
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

function getDownloadUrl(platform: string): string {
  return `${baseDownloadUrl}/v${getRuntimeVersion()}/${platform}.zip`;
}

function getRuntimeDependenciesPackages(): Package[] {
  if (runtimeJson.runtimeDependencies) {
    return JSON.parse(JSON.stringify(<Package[]>runtimeJson.runtimeDependencies));
  }
  throw new Error("No runtime dependencies found");
}

export function getCurrentPlatform(): string {
  const binPathSegmentOS = process.platform === 'win32' ? windowsPlatform : process.platform === 'darwin' ? osxPlatform : linuxPlatform;
  return `${binPathSegmentOS}-${process.arch}`;
}