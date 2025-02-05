import { getKiotaVersion } from "../getKiotaVersion";
import { ensureKiotaIsPresent } from "../install";

jest.setTimeout(60000);

describe("getKiotaVersion", () => {

  beforeAll(async () => {
    console.log("Running setup before all tests");
    await ensureKiotaIsPresent();
    console.log("ensured Kiota Is Present");
  });

  test("should return version when successful", async () => {
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });
});