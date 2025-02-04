import { getKiotaVersion } from "../getKiotaVersion";

describe("getKiotaVersion", () => {
  test("should return version when successful", async () => {
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });
});