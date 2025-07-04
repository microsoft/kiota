import { getLanguageInformationForDescription, getLanguageInformationInternal } from '../../lib/languageInformation';
import { setupKiotaStubs } from './stubs.util';

const sampleLanguageInfo = {
  CSharp: {
    MaturityLevel: 2,
    SupportExperience: 0,
    Dependencies: [
      {
        Name: "Microsoft.Kiota.Abstractions",
        Version: "1.16.4",
        DependencyType: 0,
      }
    ],
    DependencyInstallCommand: "dotnet add package {0} --version {1}",
    ClientClassName: "",
    ClientNamespaceName: "",
    StructuredMimeTypes: [
    ],
  }
};

describe("Language Information", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return language information for a description when successful', async () => {
    const mockResults = sampleLanguageInfo;
    connectionStub.mockResolvedValue(mockResults);
    const results = await getLanguageInformationForDescription({ clearCache: false, descriptionUrl: 'test.com' });
    expect(results).toBeDefined();
  });

  test('should return internal language information when successful', async () => {
    const mockResults = sampleLanguageInfo;

    connectionStub.mockResolvedValue(mockResults);
    const results = await getLanguageInformationInternal();
    expect(results).toBeDefined();
  });

});