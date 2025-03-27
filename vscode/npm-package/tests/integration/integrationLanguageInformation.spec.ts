import { getLanguageInformationForDescription, getLanguageInformationInternal } from '../../lib/languageInformation';

describe("Language Information", () => {
  test('should return language information for a description when successful', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const actual = await getLanguageInformationForDescription({ clearCache: false, descriptionUrl: descriptionUrl });
    expect(actual).toBeDefined();

    const actualCSharp = actual!.CSharp;
    expect(actualCSharp).toBeDefined();
    const actualCLI = actual!.CLI;
    expect(actualCLI).toBeDefined();
    const actualJava = actual!.Java;
    expect(actualJava).toBeDefined();
    const actualMissingLanguage = actual!.MissingLanguage;
    expect(actualMissingLanguage).toBeUndefined();
  });

  test('should return internal language information when successful', async () => {
    const actual = await getLanguageInformationInternal();
    expect(actual).toBeDefined();

    const actualCSharp = actual!.CSharp;
    expect(actualCSharp).toBeDefined();
    const actualCLI = actual!.CLI;
    expect(actualCLI).toBeDefined();
    const actualJava = actual!.Java;
    expect(actualJava).toBeDefined();
    const actualMissingLanguage = actual!.MissingLanguage;
    expect(actualMissingLanguage).toBeUndefined();
  });

});