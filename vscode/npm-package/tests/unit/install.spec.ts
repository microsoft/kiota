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
  chmodSync: jest.fn(),
  createReadStream: jest.fn(),
  createWriteStream: jest.fn(),
}));

// Mock https module to prevent actual downloads
jest.mock('https', () => ({
  get: jest.fn(),
}));

// Mock adm-zip to prevent actual zip operations
jest.mock('adm-zip', () => {
  return jest.fn().mockImplementation(() => ({
    extractAllTo: jest.fn(),
  }));
});

// Mock crypto for hash validation
jest.mock('crypto', () => ({
  createHash: jest.fn(() => ({
    digest: jest.fn(() => 'test-hash'),
    destroy: jest.fn(),
  })),
}));

// We need to import after mocking
import { ensureKiotaIsPresentInPath, Package } from '../../install';

describe('install async file operations', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Set environment variable to simulate local zip file instead of download
    process.env.KIOTA_SIDELOADING_BINARY_ZIP_PATH = '/fake/zip/path.zip';
  });

  afterEach(() => {
    // Clean up environment variable
    delete process.env.KIOTA_SIDELOADING_BINARY_ZIP_PATH;
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
    // Mock that the zip file exists for copying
    mockFsPromises.access.mockImplementation((path) => {
      if (path === '/test/install/path') {
        return Promise.reject(new Error('ENOENT'));
      } else if (path === '/fake/zip/path.zip') {
        return Promise.resolve(undefined);
      }
      return Promise.reject(new Error('ENOENT'));
    });

    const installPath = '/test/install/path';
    const runtimeDependencies: Package[] = [
      { platformId: 'test-platform', sha256: 'test-hash' }
    ];
    const currentPlatform = 'test-platform';

    await ensureKiotaIsPresentInPath(installPath, runtimeDependencies, currentPlatform);

    // Verify that checkFileExists was called for the install path
    expect(mockFsPromises.access).toHaveBeenCalledWith(installPath);
    // Should attempt to create directory since it doesn't exist
    expect(fs.mkdirSync).toHaveBeenCalledWith(installPath, { recursive: true });
    // Should copy the zip file
    expect(fs.copyFileSync).toHaveBeenCalled();
  });

  test('should attempt installation when directory exists but is empty', async () => {
    const mockFsPromises = fs.promises as jest.Mocked<typeof fs.promises>;
    
    // Mock directory exists (access succeeds) but zip file also exists
    mockFsPromises.access.mockImplementation((path) => {
      if (path === '/test/install/path') {
        return Promise.resolve(undefined);
      } else if (path === '/fake/zip/path.zip') {
        return Promise.resolve(undefined);
      }
      return Promise.reject(new Error('ENOENT'));
    });
    // Mock directory is empty
    mockFsPromises.readdir.mockResolvedValue([] as any);

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
    // Should attempt to create directory since it exists but is empty
    expect(fs.mkdirSync).toHaveBeenCalledWith(installPath, { recursive: true });
  });
});