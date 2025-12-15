import AdmZip from 'adm-zip';
import { createHash } from 'crypto';
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

/**
 * Checks if a file or directory exists asynchronously
 * @param filePath Path to check
 * @returns Promise<boolean> true if exists, false otherwise
 */
async function checkFileExists(filePath: string): Promise<boolean> {
  try {
    await fs.promises.access(filePath);
    return true;
  } catch {
    return false;
  }
}

/**
 * Checks if a directory is empty asynchronously
 * @param dirPath Directory path to check
 * @returns Promise<boolean> true if directory is empty or doesn't exist, false otherwise
 */
async function isDirectoryEmpty(dirPath: string): Promise<boolean> {
  try {
    const files = await fs.promises.readdir(dirPath);
    return files.length === 0;
  } catch {
    // If directory doesn't exist, consider it as "empty" for our use case
    return true;
  }
}

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
    const runtimeDependencies = getRuntimeDependenciesPackages();
    const currentPlatform = getCurrentPlatform();
    await ensureKiotaIsPresentInPath(installPath, runtimeDependencies, currentPlatform);
  }
}

export async function ensureKiotaIsPresentInPath(installPath: string, runtimeDependencies: Package[], currentPlatform: string) {
  if (installPath) {
    if (!await checkFileExists(installPath) || await isDirectoryEmpty(installPath)) {
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
          if (kiotaBinaryZip && await checkFileExists(kiotaBinaryZip)) {
            fs.copyFileSync(kiotaBinaryZip, zipFilePath);
          } else {
            const downloadUrl = getDownloadUrl(currentPlatform);
            await downloadFileFromUrl(downloadUrl, zipFilePath);
            if (!await doesFileHashMatch(zipFilePath, packageToInstall.sha256)) {
              throw new Error("Hash validation of the downloaded file mismatch");
            }
          }
          unzipFile(zipFilePath, installPath);
          if ((currentPlatform.startsWith(linuxPlatform) || currentPlatform.startsWith(osxPlatform)) && installPath) {
            const fileName = getKiotaFileName();
            const kiotaFilePath = path.join(installPath, fileName);
            makeExecutable(kiotaFilePath);
          }
        } catch (error) {
          fs.rmSync(installPath, { recursive: true, force: true });
          throw new Error("Kiota download failed. Check the logs for more information.");
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

function getKiotaFileName(): string {
  return process.platform === 'win32' ? 'kiota.exe' : 'kiota';
}

function getKiotaPathInternal(withFileName = true): string | undefined {
  const fileName = getKiotaFileName();
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

async function doesFileHashMatch(destinationPath: string, hashValue: string): Promise<boolean> {
  const hash = createHash('sha256');
  return new Promise((resolve, reject) => {
    fs.createReadStream(destinationPath).pipe(hash).on('finish', () => {
      const computedValue = hash.digest('hex');
      hash.destroy();
      resolve(computedValue.toUpperCase() === hashValue.toUpperCase());
    });
  });
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

export function getRuntimeDependenciesPackages(): Package[] {
  if (runtimeJson.runtimeDependencies) {
    return JSON.parse(JSON.stringify(<Package[]>runtimeJson.runtimeDependencies));
  }
  throw new Error("No runtime dependencies found");
}

export function getCurrentPlatform(): string {
  const binPathSegmentOS = process.platform === 'win32' ? windowsPlatform : process.platform === 'darwin' ? osxPlatform : linuxPlatform;
  return `${binPathSegmentOS}-${process.arch}`;
}
