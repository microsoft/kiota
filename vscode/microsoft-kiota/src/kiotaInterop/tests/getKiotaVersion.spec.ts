import { getKiotaVersion } from "../getKiotaVersion";

jest.setTimeout(60000);

describe("getKiotaVersion", () => {
  test("should return version when successful", async () => {
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });
});