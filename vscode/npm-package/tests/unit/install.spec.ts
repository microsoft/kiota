import * as fs from 'fs';
import * as path from 'path';

// Mock fs to avoid actual file operations in tests
jest.mock('fs', () => ({
  promises: {
    access: jest.fn(),
    readdir: jest.fn(),
  },
  mkdirSync: jest.fn(),
  copyFileSync: jest.fn(),
  rmdirSync: jest.fn(),
}));

// We need to import after mocking
import { ensureKiotaIsPresentInPath, Package } from '../../install';

describe('install async file operations', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('should not attempt installation when directory exists and is not empty', async () => {
    const mockFsPromises = fs.promises as jest.Mocked<typeof fs.promises>;
    
    // Mock directory exists (access succeeds)
    mockFsPromises.access.mockResolvedValue(undefined);
    // Mock directory is not empty
    mockFsPromises.readdir.mockResolvedValue(['file1.txt'] as any);

    const installPath = '/test/install/path';
    const runtimeDependencies: Package[] = [
      { platformId: 'test-platform', sha256: 'test-hash' }
    ];
    const currentPlatform = 'test-platform';

    await ensureKiotaIsPresentInPath(installPath, runtimeDependencies, currentPlatform);

    // Verify that checkFileExists was called
    expect(mockFsPromises.access).toHaveBeenCalledWith(installPath);
    // Verify that readdir was called to check if directory is empty
    expect(mockFsPromises.readdir).toHaveBeenCalledWith(installPath);
    // Should not attempt to create directory since it exists and is not empty
    expect(fs.mkdirSync).not.toHaveBeenCalled();
  });

  test('should attempt installation when directory does not exist', async () => {
    const mockFsPromises = fs.promises as jest.Mocked<typeof fs.promises>;
    
    // Mock directory does not exist (access throws)
    mockFsPromises.access.mockRejectedValue(new Error('ENOENT'));
    // readdir won't be called since access failed, but our isDirectoryEmpty should return true

    const installPath = '/test/install/path';
    const runtimeDependencies: Package[] = [
      { platformId: 'test-platform', sha256: 'test-hash' }
    ];
    const currentPlatform = 'test-platform';

    // We expect this to throw because we haven't mocked the download process
    try {
      await ensureKiotaIsPresentInPath(installPath, runtimeDependencies, currentPlatform);
    } catch (error) {
      // Expected to fail at download since we're only testing the file existence logic
    }

    // Verify that checkFileExists was called
    expect(mockFsPromises.access).toHaveBeenCalledWith(installPath);
    // Should attempt to create directory since it doesn't exist
    expect(fs.mkdirSync).toHaveBeenCalledWith(installPath, { recursive: true });
  });

  test('should attempt installation when directory exists but is empty', async () => {
    const mockFsPromises = fs.promises as jest.Mocked<typeof fs.promises>;
    
    // Mock directory exists (access succeeds)
    mockFsPromises.access.mockResolvedValue(undefined);
    // Mock directory is empty
    mockFsPromises.readdir.mockResolvedValue([] as any);

    const installPath = '/test/install/path';
    const runtimeDependencies: Package[] = [
      { platformId: 'test-platform', sha256: 'test-hash' }
    ];
    const currentPlatform = 'test-platform';

    // We expect this to throw because we haven't mocked the download process
    try {
      await ensureKiotaIsPresentInPath(installPath, runtimeDependencies, currentPlatform);
    } catch (error) {
      // Expected to fail at download since we're only testing the file existence logic
    }

    // Verify that checkFileExists was called
    expect(mockFsPromises.access).toHaveBeenCalledWith(installPath);
    // Verify that readdir was called to check if directory is empty
    expect(mockFsPromises.readdir).toHaveBeenCalledWith(installPath);
    // Should attempt to create directory since it exists but is empty
    expect(fs.mkdirSync).toHaveBeenCalledWith(installPath, { recursive: true });
  });
});