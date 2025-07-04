import { getKiotaVersion } from "../../lib/getKiotaVersion";

describe("getKiotaVersionIntegration", () => {
  test('should return version when successful', async () => {
    const expectedExpression: RegExp = new RegExp("^1.\\d+.\\d+$");

    const actual = await getKiotaVersion();
    expect(actual).toMatch(expectedExpression);
  });

});
