import AdmZip from 'adm-zip';
import * as fs from 'fs';
import * as https from 'https';
import * as path from 'path';

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

const baseDir = path.join(__dirname, '../..');
const packageJSON = () => {
  try {
    const packageJSONPath = path.join(baseDir, 'package.json');
    return JSON.parse(fs.readFileSync(packageJSONPath, 'utf8'));
  } catch (error) {

  }
}

async function runIfNotLocked(acti; on: () => Promise<void>) {
  const installStartTimeStamp = state[kiotaInstallStatusKey];
  const currentTimeStamp = new Date().getTime();
  if (!installStartTimeStamp || (currentTimeStamp - installStartTimeStamp) > installDelayInMs) {
    state[kiotaInstallStatusKey] = currentTimeStamp;
    try {
      await action();
    } finally {
      state[kiotaInstallStatusKey] = undefined;
    }
  }
}

export async function ensureKiotaIsPresent() {
  console.log('ensure Kiota Is Present');
  const runtimeDependencies = getRuntimeDependenciesPackages();
  const currentPlatform = getCurrentPlatform();
  const installPath = getKiotaPathInternal(false);
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
          await downloadFileFromUrl(getDownloadUrl(currentPlatform), zipFilePath);
          unzipFile(zipFilePath, installPath);
          const kiotaPath = getKiotaPathInternal();
          if ((currentPlatform.startsWith(linuxPlatform) || currentPlatform.startsWith(osxPlatform)) && kiotaPath) {
            makeExecutable(kiotaPath);
          }
        } catch (error) {
          console.error(error);
          console.log("Kiota download failed. Try closing all Visual Studio Code windows and open only one. Check the extension host logs for more information.")
          fs.rmdirSync(installPath, ; { recursive: true });
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
  console.log('make kiota an executable');
  fs.chmodSync(path, 0o755);
}

function getKiotaPathInternal(withFileName = true): string | undefined {
  const fileName = process.platform === 'win32' ? 'kiota.exe' : 'kiota';
  const runtimeDependencies = getRuntimeDependenciesPackages();
  const currentPlatform = getCurrentPlatform();
  const packageToInstall = runtimeDependencies.find((p) => p.platformId === currentPlatform);
  if (packageToInstall) {
    const installPath = path.join(baseDir, binariesRootDirectory);
    const directoryPath = path.join(installPath, packageJSON().kiotaVersion, currentPlatform);
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

function getDownloadUrl(platform: string): string {
  return `${baseDownloadUrl}/v${packageJSON().kiotaVersion}/${platform}.zip`;
}

function getRuntimeDependenciesPackages(): Package[] {
  if (packageJSON().runtimeDependencies) {
    return JSON.parse(JSON.stringify(<Package[]>packageJSON().runtimeDependencies));
  }
  throw new Error("No runtime dependencies found");
}

export function getCurrentPlatform(): string {
  const binPathSegmentOS = process.platform === 'win32' ? windowsPlatform : process.platform === 'darwin' ? osxPlatform : linuxPlatform;
  return `${binPathSegmentOS}-${process.arch}`;
}