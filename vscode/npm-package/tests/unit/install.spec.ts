import * as https from 'https';
import * as fs from 'fs';
import { EventEmitter } from 'events';

// Need to mock these modules before importing the install module
jest.mock('https');
jest.mock('fs');

// Import after mocking
import { ensureKiotaIsPresentInPath, getCurrentPlatform, Package } from '../../install';

const mockHttps = https as jest.Mocked<typeof https>;
const mockFs = fs as jest.Mocked<typeof fs>;

describe('install', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('downloadFileFromUrl redirect handling', () => {
    test('should handle successful download without redirects', async () => {
      // Mock file operations
      mockFs.existsSync.mockReturnValue(false);
      mockFs.readdirSync.mockReturnValue([]);
      mockFs.mkdirSync.mockImplementation();
      mockFs.createWriteStream.mockReturnValue({
        on: jest.fn((event: string, callback: Function) => {
          if (event === 'finish') {
            setTimeout(callback, 0);
          }
        }),
        close: jest.fn(),
        pipe: jest.fn()
      } as any);

      // Mock successful HTTPS response
      const mockResponse = new EventEmitter();
      (mockResponse as any).statusCode = 200;
      (mockResponse as any).pipe = jest.fn();

      mockHttps.get.mockImplementation((url: any, callback: any) => {
        setTimeout(() => callback(mockResponse), 0);
        return {
          on: jest.fn((event: string, callback: Function) => {
            // Don't trigger error
          })
        } as any;
      });

      // Mock hash validation
      const mockHash = {
        digest: jest.fn().mockReturnValue('TESTHASH'),
        destroy: jest.fn(),
        on: jest.fn((event: string, callback: Function) => {
          if (event === 'finish') {
            setTimeout(callback, 0);
          }
        }),
        pipe: jest.fn().mockReturnThis()
      };
      
      const mockCreateReadStream = {
        pipe: jest.fn().mockReturnValue(mockHash)
      };
      mockFs.createReadStream.mockReturnValue(mockCreateReadStream as any);

      // Mock AdmZip
      jest.doMock('adm-zip', () => {
        return jest.fn().mockImplementation(() => ({
          extractAllTo: jest.fn()
        }));
      });

      const testPackage: Package = {
        platformId: 'test-platform',
        sha256: 'TESTHASH'
      };

      // This should not throw an error
      await expect(ensureKiotaIsPresentInPath('/test/path', [testPackage], 'test-platform')).resolves.not.toThrow();
    });

    test('should handle limited redirects correctly', async () => {
      // Mock file operations  
      mockFs.existsSync.mockReturnValue(false);
      mockFs.readdirSync.mockReturnValue([]);
      mockFs.mkdirSync.mockImplementation();
      mockFs.createWriteStream.mockReturnValue({
        on: jest.fn((event: string, callback: Function) => {
          if (event === 'finish') {
            setTimeout(callback, 0);
          }
        }),
        close: jest.fn(),
        pipe: jest.fn()
      } as any);

      let redirectCount = 0;
      const maxRedirects = 3;

      mockHttps.get.mockImplementation((url: any, callback: any) => {
        const mockResponse = new EventEmitter();
        
        if (redirectCount < maxRedirects) {
          // Return redirect response
          (mockResponse as any).statusCode = 302;
          (mockResponse as any).headers = { location: `http://redirect-${redirectCount}.com/file.zip` };
          redirectCount++;
        } else {
          // Final successful response
          (mockResponse as any).statusCode = 200;
          (mockResponse as any).pipe = jest.fn();
        }

        setTimeout(() => callback(mockResponse), 0);
        return {
          on: jest.fn((event: string, callback: Function) => {
            // Don't trigger error
          })
        } as any;
      });

      // Mock hash validation
      const mockHash = {
        digest: jest.fn().mockReturnValue('TESTHASH'),
        destroy: jest.fn(),
        on: jest.fn((event: string, callback: Function) => {
          if (event === 'finish') {
            setTimeout(callback, 0);
          }
        }),
        pipe: jest.fn().mockReturnThis()
      };
      
      const mockCreateReadStream = {
        pipe: jest.fn().mockReturnValue(mockHash)
      };
      mockFs.createReadStream.mockReturnValue(mockCreateReadStream as any);

      // Mock AdmZip
      jest.doMock('adm-zip', () => {
        return jest.fn().mockImplementation(() => ({
          extractAllTo: jest.fn()
        }));
      });

      const testPackage: Package = {
        platformId: 'test-platform',
        sha256: 'TESTHASH'
      };

      // This should complete successfully after following redirects
      await expect(ensureKiotaIsPresentInPath('/test/path', [testPackage], 'test-platform')).resolves.not.toThrow();
      
      // Verify that we made the expected number of HTTP calls (initial + redirects + final)
      expect(mockHttps.get).toHaveBeenCalledTimes(maxRedirects + 1);
    });

    test('should reject when redirect limit is exceeded', async () => {
      // Mock file operations
      mockFs.existsSync.mockReturnValue(false);
      mockFs.readdirSync.mockReturnValue([]);
      mockFs.mkdirSync.mockImplementation();
      mockFs.rmdirSync.mockImplementation();

      // Mock infinite redirects
      mockHttps.get.mockImplementation((url: any, callback: any) => {
        const mockResponse = new EventEmitter();
        (mockResponse as any).statusCode = 302;
        (mockResponse as any).headers = { location: 'http://redirect-loop.com/file.zip' };

        setTimeout(() => callback(mockResponse), 0);
        return {
          on: jest.fn((event: string, callback: Function) => {
            // Don't trigger error initially
          })
        } as any;
      });

      const testPackage: Package = {
        platformId: 'test-platform',
        sha256: 'TESTHASH'
      };

      // This should throw an error due to too many redirects
      await expect(ensureKiotaIsPresentInPath('/test/path', [testPackage], 'test-platform'))
        .rejects.toThrow('Kiota download failed. Check the logs for more information.');
      
      // Verify that cleanup was called
      expect(mockFs.rmdirSync).toHaveBeenCalledWith('/test/path', { recursive: true });
    });
  });
});