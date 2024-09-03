import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

export function createTemporaryFolder(): string | undefined {
  const uniqueFolderIdentifier = `kiota-${Date.now()}`;
  const temporaryDirectory = os.tmpdir();
  const temporaryDirectoryPath = path.join(temporaryDirectory, uniqueFolderIdentifier);

  try {
    createFolderInFileSystem(temporaryDirectoryPath);
    return path.resolve(temporaryDirectoryPath);
  } catch (error) {
    return undefined;
  }
}

function createFolderInFileSystem(directoryPath: string) {
  const exists = fs.existsSync(directoryPath);
  if (exists) {
    return createTemporaryFolder();
  }

  try {
    fs.mkdirSync(directoryPath);
  } catch (err: unknown) {
    throw new Error(`Error creating temporary directory: ${(err as Error).message}`);
  }
}

export function isTemporaryDirectory(path: string): boolean {
  return path.startsWith(os.tmpdir());
}