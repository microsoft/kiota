import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

export function createTemporaryFolder(): string {
  const temporaryDirectory = os.tmpdir();
  const temporaryDirectoryPath = path.join(temporaryDirectory, `kiota-${Date.now()}`);
  fs.mkdirSync(temporaryDirectoryPath);
  return temporaryDirectoryPath;
}

export function isTemporaryDirectory(path: string): boolean {
  return path.startsWith(os.tmpdir());
}