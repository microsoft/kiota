import { getKiotaVersion } from "../getKiotaVersion";
import { https } from "follow-redirects";

jest.setTimeout(60000);

describe("getKiotaVersion", () => {
  let request: ReturnType<typeof https.get> | undefined;

  beforeEach(() => {
    request = undefined;
  });

  afterEach(() => {
    if (request) {
      request.destroy();
    }
  });

  test("should return version when successful", async () => {
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });
});